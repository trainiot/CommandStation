﻿using System;
using System.Linq;
using System.Text;

namespace Trainiot.CommandStation.Dcc
{
    /// <summary>
    ///   Represents a DCC command
    /// </summary>
    /// <remarks>
    ///   This is implemented as a readonly struct to signal intent (immutable), not to get copy
    ///   performance increases as a DCC command will never be significently longer than the
    ///   corresponding array reference.
    /// </remarks>
    public readonly struct DccPacket
    {
        public DccPacket(ReadOnlySpan<byte> packetBytes)
        {
            this.PacketBytes = packetBytes;
            if (packetBytes.Length < 3)
            {
                throw new DccPacketException(DccPacketInvalidReason.TooShort);
            }
            else if (packetBytes.Length > 6)
            {
                throw new DccPacketException(DccPacketInvalidReason.TooLong);
            }

            int checksum = 0;
            for (int i = 0; i < packetBytes.Length - 1; i++)
            {
                checksum ^= packetBytes[i];
            }

            if (checksum != packetBytes[packetBytes.Length - 1])
            {
                throw new DccPacketException(DccPacketInvalidReason.Checksum);
            }
        }

        public static DccPacket IdlePacket { get; } = new DccPacket(new byte[] { 255, 0, 255 });

        public static DccPacket ResetPacket { get; } = new DccPacket(new byte[] { 0, 0, 0 });

        // This is not a standard DCC packaget, but an extension. Consider moving it somewhere else.
        public static DccPacket OffPacket { get; } = new DccPacket(new byte[0]);

        public ReadOnlySpan<byte> PacketBytes { get; }

        public int Address
        {
            get
            {
                byte b0 = PacketBytes[0];
                if ((b0 & 0x80) == 0)
                {
                    // Baseline Packet
                    return PacketBytes[0];
                }

                if (b0 == 255)
                {
                    return 255;
                }

                if ((b0 & 0b010_0000) == 0)
                {
                    // Multifunction Decoder Packet
                    return ((b0 & 0b0011_1111) << 6) | PacketBytes[1];
                }
                else 
                {
                    // Accessory Decoder Packet
                    byte b1 = PacketBytes[1];
                    if ((b1 & 0b1000_0000) != 0)
                    {
                        // Basic Accessory Decoder Packet
                        return ((b1 & 0b0111_0000) << 2) | (b0 & 0b0011_1111);
                    }
                    else 
                    {
                        // Extended Accessory Decoder Packet
                        // The docs do NOT make it clear what the most significent bit is. I am just guessing here,
                        // it is not like there is any consistency to copy a pattern from :)
                        return ((b1 & 0b0111_0000) << 4) | ((b1 & 0b0000_1100) << 4) | (b0 & 0b0011_1111);
                    }
                }
            }
        }

        public bool IsForAccessoryDecoder
        {
            get
            {
                var a = PacketBytes[0];
                return a >= 128 && a <= 191;
            }
        }

        public bool IsForMultiFunctionDecoder
        {
            get
            {
                var a = PacketBytes[0];
                return ((a >= 1 && a <= 127) || (a >= 192 && a <= 231));
            }
        }

        public bool IsIdlePacket => PacketBytes[0] == 255;

        public bool IsBroadcastPacket => PacketBytes[0] == 0;

        public static bool operator==(DccPacket p1, DccPacket p2)
        {
            return p1.PacketBytes.SequenceEqual(p2.PacketBytes);
        }

        public static bool operator!=(DccPacket p1, DccPacket p2)
        {
            return !p1.PacketBytes.SequenceEqual(p2.PacketBytes);
        }

        public override bool Equals(object obj)
        {
            if (obj is DccPacket)
            {
                return false;
            }

            DccPacket other = (DccPacket)obj;
            return other == this;
        }

        public override int GetHashCode()
        {
            int length = PacketBytes.Length - 1; // No need to include the checksum
            long result = 0;
            for (int index = 0; index < length; index++)
            {
                result = (result << 8) | PacketBytes[index];
            }

            return result.GetHashCode();
        }

        public override string ToString()
        {
            if (PacketBytes.Length == 0)
            {
                return "[OFF]";
            }

            StringBuilder sb = new StringBuilder(PacketBytes.Length * 3 + 1);
            sb.Append('[');
            for (int i = 0; i < PacketBytes.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(PacketBytes[i].ToString("X2"));
            }

            sb.Append(']');
            return sb.ToString();
        }
    }
}