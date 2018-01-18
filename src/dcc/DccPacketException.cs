using System;
using System.Collections.Generic;
using System.Text;

namespace CommandStation.Dcc
{
    public class DccPacketException : Exception
    {
        public DccPacketException(DccPacketInvalidReason reason, string message = null) :
            base()
        {
            Reason = reason;
        }

        public DccPacketInvalidReason Reason { get; }

        private static string GetDefaultMessage(DccPacketInvalidReason reason)
        {
            switch (reason)
            {
                case DccPacketInvalidReason.Checksum:
                    return "Invalid checksum byte";
                case DccPacketInvalidReason.TooLong:
                    return "The packet exceeds the allowed length of 6 bytes.";
                case DccPacketInvalidReason.TooShort:
                    return "The packet is shorter than the required minimum 3 bytes.";
            }


            return $"Invalid DCC packet (reason: {reason})";
            
        }
    }
}
