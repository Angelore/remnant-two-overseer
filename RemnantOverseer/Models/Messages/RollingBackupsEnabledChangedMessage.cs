using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RemnantOverseer.Models.Messages;
internal class RollingBackupsEnabledChangedMessage(bool value): ValueChangedMessage<bool>(value)
{
}
