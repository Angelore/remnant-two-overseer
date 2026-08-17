using System;

namespace RemnantOverseer.Models;

public class BloodmoonInfo
{
    // All times are stored in GMT (UTC) as provided by the save file.
    public double CurrentChance { get; set; }
    public DateTime LastTriggeredTime { get; set; }
    public DateTime LastCheckTime { get; set; }
    public int ZoneLoadCount { get; set; }
}
