using System;
using System.Collections.Generic;
using System.Text;

namespace Trainiot.CommandStation.Dcc
{
    public enum DccPacketInvalidReason
    {
        Unknown,
        TooShort,
        TooLong,
        Checksum,
    }
}
