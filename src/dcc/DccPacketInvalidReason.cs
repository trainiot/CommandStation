using System;
using System.Collections.Generic;
using System.Text;

namespace CommandStation.Dcc
{
    public enum DccPacketInvalidReason
    {
        Unknown,
        TooShort,
        TooLong,
        Checksum,
    }
}
