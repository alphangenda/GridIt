using Application.Extensions;
using Domain.Common;
using Domain.Extensions;
using Domain.Repositories;
using FastEndpoints;

namespace Web.Features.Public.Authentication.ConfirmEmail;

public class ConfirmEmailEndpoint : Endpoint<ConfirmEmailRequest, SucceededOrNotResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ConfirmEmailEndpoint> _logger;

    public ConfirmEmailEndpoint(
        IUserRepository userRepository,
        ILogger<ConfirmEmailEndpoint> logger)
    {
        _logger = logger;
        _userRepository = userRepository;
    }

    public override void Configure()
    {
        DontCatchExceptions();
        Post("authentication/confirm-email");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ConfirmEmailRequest req, CancellationToken ct)
    {
        var user = _userRepository.FindById(req.UserId);
        if (user == null)
        {
            _logger.LogInformation("Could not confirm email since no user with id {id} exists.", req.UserId);
            await Send.OkAsync(new SucceededOrNotResponse(false), ct);
            return;
        }

        var identityResult = await _userRepository.ConfirmUserEmail(user, req.Token.Base64UrlDecode());
        if (!identityResult.Succeeded)
        {
            await Send.OkAsync(new SucceededOrNotResponse(false, identityResult.GetErrors()), ct);
            return;
        }

        await Send.OkAsync(new SucceededOrNotResponse(true), ct);
    }
}