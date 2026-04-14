using Application.Extensions;
using Application.Interfaces.Services.Notifications;
using Application.Interfaces.Services.Users;
using Application.Settings;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.Identity;
using Domain.Repositories;
using Microsoft.Extensions.Options;
using Web.Features.Common;

namespace Web.Features.Public.Authentication.Register;

public class RegisterEndpoint : EndpointWithSanitizedRequest<RegisterRequest, SucceededOrNotResponse>
{
    private readonly string _baseUrl;
    private readonly IUserRepository _userRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<RegisterEndpoint> _logger;
    private readonly INotificationService _notificationService;

    public RegisterEndpoint(
        IUserRepository userRepository,
        IMemberRepository memberRepository,
        ILogger<RegisterEndpoint> logger,
        INotificationService notificationService,
        IAuthenticationService authenticationService,
        IOptions<ApplicationSettings> applicationSettings)
    {
        _logger = logger;
        _userRepository = userRepository;
        _memberRepository = memberRepository;
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
        if (!IsValidEmail(req.Email))
        {
            await Send.OkAsync(
                new SucceededOrNotResponse(false,
                    new Error("InvalidEmail", "The email format is invalid.")
                ), ct);
            return;
        }

        var tempUser = new User { Email = req.Email, UserName = req.Email };
        if (!_authenticationService.IsTeacherFromPublicCegep(tempUser))
        {
            await Send.OkAsync(
                new SucceededOrNotResponse(false,
                    new Error("Forbidden", "You must be a teacher from a public cegep to register.")
                ), ct);
            return;
        }

        if (_userRepository.UserWithEmailExists(req.Email))
        {
            await Send.OkAsync(
                new SucceededOrNotResponse(false,
                    new Error("EmailAlreadyExists", "A user with this email already exists.")
                ), ct);
            return;
        }

        if (!IsValidPassword(req.Password))
        {
            await Send.OkAsync(
                new SucceededOrNotResponse(false,
                    new Error("InvalidPassword",
                        "The password must contain at least 8 characters, including an uppercase letter, a lowercase letter, a number, and a special character.")
                ), ct);
            return;
        }

        var user = new User { Email = req.Email, UserName = req.Email, TwoFactorEnabled = true };
        user.AddRole(new Role { Name = Domain.Constants.User.Roles.MEMBER });

        User createdUser;
        try
        {
            createdUser = await _userRepository.CreateUser(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user with email {Email}", req.Email);
            await Send.OkAsync(new SucceededOrNotResponse(false, new Error("CreateUserFailed", "An error occurred while creating the account. Please try again.")), ct);
            return;
        }

        var passwordCreated = await _userRepository.CreateUserPassword(createdUser, req.Password);
        if (!passwordCreated.Succeeded)
        {
            await _userRepository.HardDeleteUser(createdUser);
            var errors = passwordCreated.Errors.Select(e => new Error(e.Code ?? "InvalidPassword", e.Description ?? "The password is not valid."));
            await Send.OkAsync(new SucceededOrNotResponse(false, errors), ct);
            return;
        }

        var emailPrefix = req.Email.Split('@')[0];
        var member = new Member(emailPrefix, emailPrefix);
        member.OnCreated(createdUser);
        await _memberRepository.Create(member);

        var token = await _userRepository.GetEmailConfirmationTokenForUser(createdUser);
        var link = $"{_baseUrl}{req.ConfirmEmailRelativeUrl}?userId={createdUser.Id}&token={token.Base64UrlEncode()}";
        var response = await _notificationService.SendRegisterConfirmationNotification(createdUser, link);

        if (!response.Succeeded)
        {
            await _userRepository.HardDeleteUser(createdUser);
            await Send.OkAsync(new SucceededOrNotResponse(false, response.Errors), ct);
            return;
        }

        await Send.OkAsync(new SucceededOrNotResponse(true), ct);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (password.Length < 8)
            return false;

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}