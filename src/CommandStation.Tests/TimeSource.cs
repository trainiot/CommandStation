using System;

namespace Trainiot.CommandStation.Tests
{
    public class TimeSource : ITimeSource
    {
        DateTime time;

        public TimeSource() => time = DateTime.UtcNow;

        public void Advance(TimeSpan delta) => time = time + delta;

        public DateTime UtcNow => time;
    }
}