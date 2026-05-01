using System.Collections.Generic;

using RemnantOverseer.Services;

namespace RemnantOverseer.Models;
public class Zone
{
    public string CanonicalName { get; set; } = string.Empty;
    public string Name
    {
        get => LocalizationService.GameString(CanonicalName, CanonicalName);
        set => CanonicalName = value.Trim();
    }
    public string Story { get; set; } = string.Empty;
    public List<Location> Locations { get; set; } = [];

    public Zone ShallowCopy()
    {
        return (Zone)MemberwiseClone();
    }
}
