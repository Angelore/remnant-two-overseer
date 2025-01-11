namespace RemnantOverseer.Utilities;
internal static class NotificationStrings
{
    public static string DefaultLocationNotFound = "Could not detect the location of the save folder. Set it manually in the settings";
    public static string DefaultLocationFound = "Save file location was found and set";
    public static string ErrorWhenUpdatingSettings = "An error was encountered while saving settings";

    public static string SaveFileParsingError = "An error was encountered while parsing the save file. Message:";
    public static string FileWatcherFolderNotFound = "The folder with the requested file was not found. Ensure that the path is correct and restart the application";

    public static string SaveFileLocationChanged = "Save file location was changed successfully";

    public static string SelectedCharacterNotValid = "An issue encountered when trying to select active wharacter. Select a character manually";

    public static string NewerVersionFound = "A new version ({0}) is available";

    public static string BackupFolderDoesntExist = "Backup folder doesn't exist. Created a new folder";
}
