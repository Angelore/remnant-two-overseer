using RemnantOverseer.Services;

namespace RemnantOverseer.Utilities;
internal static class NotificationStrings
{
    public static string DefaultLocationFound => LocalizationService.Get(nameof(DefaultLocationFound));
    public static string DefaultLocationNotFound => LocalizationService.Get(nameof(DefaultLocationNotFound));
    public static string ErrorWhenLoadingSettings => LocalizationService.Get(nameof(ErrorWhenLoadingSettings));
    public static string ErrorWhenUpdatingSettings => LocalizationService.Get(nameof(ErrorWhenUpdatingSettings));

    public static string SaveFileParsingError => LocalizationService.Get(nameof(SaveFileParsingError));
    public static string FileWatcherFolderNotFound => LocalizationService.Get(nameof(FileWatcherFolderNotFound));
    public static string FileWatcherFileNotFound => LocalizationService.Get(nameof(FileWatcherFileNotFound));

    public static string SaveFileLocationChanged => LocalizationService.Get(nameof(SaveFileLocationChanged));

    public static string SelectedCharacterNotValid => LocalizationService.Get(nameof(SelectedCharacterNotValid));

    public static string NewerVersionFound => LocalizationService.Get(nameof(NewerVersionFound));
    public static string PlayerInfoNotAvailable => LocalizationService.Get(nameof(PlayerInfoNotAvailable));
}
