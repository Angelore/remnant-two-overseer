namespace RemnantOverseer.Services.Models;
public class Settings
{
    public string? SaveFilePath { get; set; }

    public string? BackupPath { get; set; }

    public byte RollingBackupAmount { get; set; }
}
