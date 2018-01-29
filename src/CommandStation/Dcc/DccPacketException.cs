using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Trainiot.CommandStation.Dcc
{
    [Serializable]
    public class DccPacketException : Exception
    {
        private static readonly string ReasonSerializationKey = typeof(DccPacketException).FullName + '.' + nameof(Reason);

        public DccPacketException(DccPacketInvalidReason reason, string message = null) :
            base()
        {
            Reason = reason;
        }

        protected DccPacketException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Reason = (DccPacketInvalidReason)info.GetInt32(ReasonSerializationKey);
        }

        public DccPacketInvalidReason Reason { get; }

        override public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(ReasonSerializationKey, (int)Reason);
        }

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
