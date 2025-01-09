using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RemnantOverseer.Models.Messages;
internal class BackupPathChangedMessage(string path): ValueChangedMessage<string>(path)
{
}
