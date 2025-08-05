namespace RemnantOverseer.Utilities;

public class ConfigData
{
    public string? SaveFilePath { get; set; }
    public bool? HideDuplicates { get; set; }
    public bool? HideLootedItems { get; set; }
    public bool? HideMissingPrerequisiteItems { get; set; }
    public bool? HideHasRequiredMaterialItems { get; set; }
    public bool? DisableVersionCheck { get; set; }
    public bool? HideTips { get; set; }
    public bool? HideToolkitLinks { get; set; }
}
