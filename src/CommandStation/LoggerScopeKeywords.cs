using System.Collections.Generic;
using Trainiot.CommandStation.Dcc;
using Trainiot.CommandStation.Transmit;

namespace Trainiot.CommandStation
{
    internal static class LoggerScopeKeywords
    {
        internal const string TransmitQueueEntryKeyword = nameof(TransmitQueueEntry);
        internal static KeyValuePair<string, object> TransmitQueueEntry(TransmitQueueEntry entry) =>
             new KeyValuePair<string, object>(TransmitQueueEntryKeyword, entry);

        internal const string DccPacketKeyword = nameof(DccPacket);
        internal static KeyValuePair<string, object> DccPacket(DccPacket packet) =>
            new KeyValuePair<string, object>(DccPacketKeyword, packet);   
    }
}