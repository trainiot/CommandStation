using System;

namespace Trainiot.CommandStation
{
    // Allows dependency injectig a time source controlled by the unit tests.
    // production will always get the time from DateTime.UtcNow.
    public interface ITimeSource
    {
         DateTime UtcNow { get; }

         
    }
}