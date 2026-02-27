using Domain.Common;
using Domain.Entities.Identity;

namespace Application.Interfaces.Services.Notifications;

public interface INotificationService
{
    Task<SucceededOrNotResponse> SendForgotPasswordNotification(User user, string link);
    Task<SucceededOrNotResponse> SendTwoFactorAuthenticationCodeNotification(User user, string code);
    Task<SucceededOrNotResponse> SendRegisterConfirmationNotification(User user, string link);
}