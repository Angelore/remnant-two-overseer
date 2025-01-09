using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RemnantOverseer.Models.Messages;
internal class RollingBackupAmountChangedMessage(byte amount): ValueChangedMessage<byte>(amount)
{
}
