using RemnantOverseer.Models.Enums;
using System;
using System.Collections.Generic;

namespace RemnantOverseer.Services.Models;
internal class BackupSettings
{
    public List<BackupSettingsEntry> RollingBackupSettingsEntries { get; set; } = [];
    public List<BackupSettingsEntry> BackupSettingsEntries { get; set; } = [];
}

internal class BackupSettingsEntry
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BackupTypes Type { get; set; }
    public DateTime BackupDate { get; set; }
}