using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RemnantOverseer.Models.Messages;
internal class RollingBackupsAmountChangedMessage(byte value): ValueChangedMessage<byte>(value)
{
}
