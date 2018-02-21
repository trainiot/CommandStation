using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trainiot.CommandStation.Dcc;
using Medallion.Collections;
using Microsoft.Extensions.Logging;

namespace Trainiot.CommandStation.Transmit
{
    internal class TransmitQueue : IDccPacketSchedular
    {
        private readonly ILogger logger;
        private Task currentProcessTask;
        private CancellationTokenSource stopTokenSource;
        private readonly PriorityQueue<TransmitQueueEntry> transmitQueue = new PriorityQueue<TransmitQueueEntry>(Comparer<TransmitQueueEntry>.Create((x, y) => x.Priority.CompareTo(y.Priority)));
        private readonly List<TransmitQueueEntry> transmitQueueReinsertList = new List<TransmitQueueEntry>();
        private readonly ConcurrentQueue<TransmitQueueEntry> intakeQueue = new ConcurrentQueue<TransmitQueueEntry>();
        private readonly ManualResetEventSlim intakeQueueContainsData = new ManualResetEventSlim();

        // A decoder can reject packages send right after each other. This dictionary tracks the last
        // command send to a specific decoder so we can determine if it is ready for the next command.
        // Identical packages are not delayed.
        private readonly Dictionary<int, DecoderQuarantineEntry> DecoderQuarantines = new Dictionary<int, DecoderQuarantineEntry>();

        // Used to clear the DecoderQuarantines queue
        private readonly PriorityQueue<DecoderQuarantineClearEntry> clearDecoderQuarantineQueue = new PriorityQueue<DecoderQuarantineClearEntry>(Comparer<DecoderQuarantineClearEntry>.Create((x, y) => x.QuarantineExpiresTime.CompareTo(y.QuarantineExpiresTime)));

        private readonly Func<DateTime> utcNow;

        private volatile bool isEnabled;
        private volatile bool isBroadcastingShutdown;

        public TransmitQueue(ILogger logger, ITimeSource timeSource)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            utcNow = timeSource == null ? (Func<DateTime>)(() => DateTime.UtcNow) : () => timeSource.UtcNow;
        }

        public void Start()
        {
            if (currentProcessTask != null)
            {
                throw new InvalidOperationException("The TransmitQueue is already running.");
            }

            stopTokenSource = new CancellationTokenSource();

            currentProcessTask = Process();
        }

        public Task Stop()
        {
            if (currentProcessTask == null)
            {
                return Task.CompletedTask;
            }

            stopTokenSource.Cancel();
            var result = currentProcessTask;
            currentProcessTask = null;
            return result;
        }

        public void Enqueue(DccPacket packet)
        {
            TimeSpan priority = TimeSpan.Zero; // TODO: Set priority based on command type.

            // Random choice - Positive numbers indicate higher priority
            long priortyLong = (utcNow() - priority).Ticks;
            TransmitQueueEntry queueEntry = new TransmitQueueEntry(packet, priortyLong);
            intakeQueue.Enqueue(queueEntry);
            intakeQueueContainsData.Set();
        }

        public Task Process()
        {
            while (!stopTokenSource.IsCancellationRequested)
            {
                try
                {
                    intakeQueueContainsData.Reset();
                    MoveCommandsFromIntakeToPriorityQueue();
                    ClearDecoderQuarantines();
                    DccPacket packet = SelectPacketToTransmit();
                    if (transmitQueue.Count == 0 && intakeQueue.Count == 0)
                    {
                        intakeQueueContainsData.Wait()

                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error transmitting DCC packet.");
                }
            }

            return Task.CompletedTask;
        }

        private DccPacket SelectPacketToTransmit()
        {
            DccPacket defaultPackage = isEnabled ? DccPacket.IdlePacket : DccPacket.OffPacket;
            DccPacket result = defaultPackage;
            while (transmitQueue.Count > 0 && result == defaultPackage)
            {
                var transmitQueueEntry = transmitQueue.Dequeue();
                if (transmitQueueEntry.IsCanceled)
                {
                    continue;
                }

                if (!IsDecoderReadyToReceivePacket(transmitQueueEntry.DccPacket))
                {
                    transmitQueueReinsertList.Add(transmitQueueEntry);
                    continue;
                }

                result = transmitQueueEntry.DccPacket;
            }

            // Place any commands taken out but not send due to quarantine back in the queue.

            transmitQueue.EnqueueRange(transmitQueueReinsertList);
            transmitQueueReinsertList.Clear();
            if (transmitQueueReinsertList.Capacity > 128)
            {
                transmitQueueReinsertList.Capacity = 64;
            }

            return DccPacket.IdlePacket;
        }

        private bool IsDecoderReadyToReceivePacket(DccPacket dccPacket)
        {
            var address = dccPacket.Address;

            // In case it is a broadcast packet, we could choose to check if any decoder is in a timeout period and wait sending the broadcast.
            // but most broadcast messages a probably repeated - so for now this is skipped.

            if (DecoderQuarantines.TryGetValue(address, out var quarantineEntry) &&
                quarantineEntry.QuarantineExpiresTime > utcNow() &&
                !quarantineEntry.DccPacket.Equals(dccPacket))
            {
                return false;
            }

            if (dccPacket.IsBroadcastPacket)
            {
                return true;
            }

            // There might still be a quarantine due to a broadcast.
            if (DecoderQuarantines.TryGetValue(address, out var broadcastQuarantineEntry) &&
                broadcastQuarantineEntry.QuarantineExpiresTime > utcNow() &&
                !broadcastQuarantineEntry.DccPacket.Equals(dccPacket))
            {
                return false;
            }

            return true;
        }

        private void MoveCommandsFromIntakeToPriorityQueue()
        {
            while (intakeQueue.TryDequeue(out var queueEntry))
            {
                if (queueEntry.IsCanceled)
                {
                    continue;
                }

                if (queueEntry.DccPacket == DccPacket.OffPacket || queueEntry.DccPacket == DccPacket.ResetPacket)
                {
                    // Cancel any existing package
                    foreach (var existing in transmitQueue)
                    {
                        existing.Cancel();
                    }
                }

                transmitQueue.Enqueue(queueEntry);
            }
        }

        private void ClearDecoderQuarantines()
        {
            while (clearDecoderQuarantineQueue.Count > 0 && clearDecoderQuarantineQueue.Peek().QuarantineExpiresTime < utcNow())
            {
                var toClear = clearDecoderQuarantineQueue.Dequeue();
                // Only remove if it has the same timestamp. If not, the dictionary contains a newer packet, and we will wait
                // for it's clear queue entry to make it here.
                if (DecoderQuarantines.TryGetValue(toClear.Address, out var lastCommand) && lastCommand.QuarantineExpiresTime == toClear.QuarantineExpiresTime)
                {
                    DecoderQuarantines.Remove(toClear.Address);
                }
            }
        }

        private struct DecoderQuarantineClearEntry
        {
            public DecoderQuarantineClearEntry(int address, DateTime quarantineExpiresTime)
            {
                Address = address;
                QuarantineExpiresTime = quarantineExpiresTime;
            }

            public int Address { get; }
            public DateTime QuarantineExpiresTime { get; }
        }

        private struct DecoderQuarantineEntry
        {
            public DecoderQuarantineEntry(DccPacket dccPacket, DateTime quarantineExpiresTime)
            {
                DccPacket = dccPacket;
                QuarantineExpiresTime = quarantineExpiresTime;
            }

            public DccPacket DccPacket { get; }
            public DateTime QuarantineExpiresTime { get; }
        }
    }
}
