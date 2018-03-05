
using Trainiot.CommandStation.Dcc;

namespace Trainiot.CommandStation.Transmit
{
    internal class TransmitQueueEntry 
    {
        private volatile bool isCanceled;

        public TransmitQueueEntry(DccPacket dccPacket, long priority)
        {
            this.Priority = priority;
            this.DccPacket = DccPacket;
        }

        public DccPacket DccPacket { get; }
        public long Priority { get;}
        public bool IsCanceled => isCanceled;
        public void Cancel() => isCanceled = true;

        public override string ToString() => DccPacket + (isCanceled ? ":CANCELLED" : "");  
    }
}
