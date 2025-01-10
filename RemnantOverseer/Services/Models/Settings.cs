namespace RemnantOverseer.Services.Models;
public class Settings
{
    public string? SaveFilePath { get; set; }

    public string? BackupsPath { get; set; }

    public bool RollingBackupsEnabled { get; set; }

    public byte RollingBackupsAmount { get; set; }

    public byte MinutesBetweenRollingBackups { get; set; }
}
