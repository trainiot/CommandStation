using Trainiot.CommandStation.Dcc;

namespace Trainiot.CommandStation
{
    public interface IDccPacketHardware
    {
        void Transmit(DccPacket packet);
    }
}