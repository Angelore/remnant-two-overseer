using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RemnantOverseer.Models.Messages;
internal class MinutesBetweenRollingBackupsChangedMessage(byte value): ValueChangedMessage<byte>(value)
{
}
