using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RemnantOverseer.Models.Messages;
public class DisableVersionCheckChangedMessage(bool value) : ValueChangedMessage<bool>(value)
{
}
