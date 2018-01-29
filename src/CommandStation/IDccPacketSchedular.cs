using Trainiot.CommandStation.Dcc;

namespace Trainiot.CommandStation
{
    public interface IDccPacketSchedular
    {
         void Enqueue(DccPacket packet);
    }
}