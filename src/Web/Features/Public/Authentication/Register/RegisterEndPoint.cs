using Application.Extensions;
using Application.Interfaces.Services.Notifications;
using Application.Interfaces.Services.Users;
using Application.Settings;
using Domain.Common;
using Domain.Entities.Identity;
using Domain.Repositories;
using Microsoft.Extensions.Options;
using Web.Features.Common;

namespace Web.Features.Public.Authentication.Register;

public class RegisterEndpoint : EndpointWithSanitizedRequest<RegisterRequest, SucceededOrNotResponse>
{
    private readonly string _baseUrl;
    private readonly IUserRepository _userRepository;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<RegisterEndpoint> _logger;
    private readonly INotificationService _notificationService;

    public RegisterEndpoint(
        IUserRepository userRepository,
        ILogger<RegisterEndpoint> logger,
        INotificationService notificationService,
        IAuthenticationService authenticationService,
        IOptions<ApplicationSettings> applicationSettings)
    {
        _logger = logger;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _authenticationService = authenticationService;
        _baseUrl = applicationSettings.Value.BaseUrl;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Post("authentication/register");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var tempUser = new User { Email = req.Email, UserName = req.Email };
        if (!_authenticationService.IsTeacherFromPublicCegep(tempUser))
        {
            var forbiddenError = new Error("Forbidden", "You must be a teacher from a public cegep to register.");
            await Send.OkAsync(new SucceededOrNotResponse(false, forbiddenError), ct);
            return;
        }

        if (_userRepository.UserWithEmailExists(req.Email))
        {
            _logger.LogInformation("Could not register since a user with email {email} already exists.", req.Email);
            await Send.OkAsync(new SucceededOrNotResponse(false, new Error("EmailAlreadyExists", "A user with this email already exists.")), ct);
            return;
        }

        var user = new User { Email = req.Email, UserName = req.Email };
        await _userRepository.CreateUser(user);
        await _userRepository.CreateUserPassword(user, req.Password);

        var token = await _userRepository.GetEmailConfirmationTokenForUser(user);
        var link = $"{_baseUrl}{req.ConfirmEmailRelativeUrl}?userId={user.Id}&token={token.Base64UrlEncode()}";
        var response = await _notificationService.SendRegisterConfirmationNotification(user, link);

        await Send.OkAsync(new SucceededOrNotResponse(response.Succeeded, response.Errors), ct);
    }
}