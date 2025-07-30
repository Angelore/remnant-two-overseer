using RemnantOverseer.Utilities;

namespace RemnantOverseer.Models;

// contains all the default settings
public class Settings
{
    public ConfigData Config { get; }

    public Settings(ConfigData config) => Config = config;

    public string? SaveFilePath
    {
        get { return Config.SaveFilePath; }
        set { Config.SaveFilePath = value; }
    }
    public bool HideDuplicates
    {
        get { return Config.HideDuplicates ?? true; }
        set { Config.HideDuplicates = value; }
    }
    public bool HideLootedItems
    {
        get { return Config.HideLootedItems ?? true; }
        set { Config.HideLootedItems = value; }
    }
    public bool HideMissingPrerequisiteItems
    {
        get { return Config.HideMissingPrerequisiteItems ?? false; }
        set { Config.HideMissingPrerequisiteItems = value; }
    }
    public bool HideHasRequiredMaterialItems
    {
        get { return Config.HideHasRequiredMaterialItems ?? false; }
        set { Config.HideHasRequiredMaterialItems = value; }
    }
    public bool DisableVersionCheck
    {
        get
        {
            return Config.DisableVersionCheck ??
# if DEBUG || REMNANTOVERSEER_NO_DEFAULT_VERSION_CHECK
                true;
# else
                false;
# endif
        }
        set { Config.DisableVersionCheck = value; }
    }
    public bool HideTips
    {
        get { return Config.HideTips ?? false; }
        set { Config.HideTips = value; }
    }
    public bool HideToolkitLinks
    {
        get { return Config.HideToolkitLinks ?? false; }
        set { Config.HideToolkitLinks = value; }
    }
}
