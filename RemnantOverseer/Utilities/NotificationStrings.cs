namespace RemnantOverseer.Utilities;
internal static class NotificationStrings
{
    // TODO: Add view names to strings to group them better?
    public static string DefaultLocationNotFound = "Could not detect the location of the save folder. Set it manually in the settings";
    public static string DefaultLocationFound = "Save file location was found and set";
    public static string ErrorWhenUpdatingSettings = "An error was encountered while saving settings";

    public static string SaveFileParsingError = "An error was encountered while parsing the save file. Message:";
    public static string FileWatcherFolderNotFound = "The folder with the requested file was not found. Ensure that the path is correct and restart the application";

    public static string SaveFileLocationChanged = "Save file location was changed successfully";
    public static string BackupsLocationChanged = "Location for backups was changed successfully";

    public static string SelectedCharacterNotValid = "An issue encountered when trying to select active wharacter. Select a character manually";

    public static string NewerVersionFound = "A new version ({0}) is available";

    public static string BackupFolderCreated = "Backup folder doesn't exist. Creating a new folder";
    public static string MasterBackupFolderCantBeCreated = "Can't create backup folder. Create it manually and restart the application. Backup service will not be active for this session";
    public static string BackupDatasetIsEmpty = "Something is wrong with the data. Backup was not saved";
    public static string BackupFolderCantBeCreated = "Can't create backup folder. Backup was not saved";
    public static string FileCopyError = "Encountered an error when copying files. Backup is incomplete";
    public static string BackupServiceInInvalidState = "Backup service failed to initialize due to invalid settings. It will try to start again when settings are updated";
    public static string ErrorWhenUpdatingBackupSettings = "An error was encountered while saving information about backups";
    public static string BackupFolderDoesntExist = "Backup folder doesn't exist";
    public static string SaveFolderDoesntExist = "Save folder doesn't exist";
    public static string ManualBackupSuccessful = "Successfully backed up to {0}";
}
