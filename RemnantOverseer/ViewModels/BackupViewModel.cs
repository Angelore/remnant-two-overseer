using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RemnantOverseer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemnantOverseer.ViewModels;
internal partial class BackupViewModel: ViewModelBase
{
    private bool _isEnabled;
    private BackupService _backupService;

    public BackupViewModel(BackupService backupService)
    {
        _backupService = backupService;
    }

    [RelayCommand]
    public async Task ManualBackupCommand()
    {
        await _backupService.SaveManualBackup();
    }
}
