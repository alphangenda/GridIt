using Application.Interfaces.Services.Notifications;
using Application.Interfaces.Services.Users;
using Application.Settings;
using Domain.Entities.Identity;
using Domain.Repositories;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Web.Features.Public.Authentication.Register;

namespace Tests.Web.Features.Public.Authentication.Register;

public class RegisterEndpointTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IMemberRepository> _memberRepository;
    private readonly Mock<INotificationService> _notificationService;
    private readonly Mock<IAuthenticationService> _authenticationService;
    private readonly RegisterEndpoint _endpoint;

    public RegisterEndpointTests()
    {
        _userRepository = new Mock<IUserRepository>();
        _memberRepository = new Mock<IMemberRepository>();
        _notificationService = new Mock<INotificationService>();
        _authenticationService = new Mock<IAuthenticationService>();

        _endpoint = Factory.Create<RegisterEndpoint>(
            _userRepository.Object,
            _memberRepository.Object,
            Mock.Of<ILogger<RegisterEndpoint>>(),
            _notificationService.Object,
            _authenticationService.Object,
            Options.Create(new ApplicationSettings { BaseUrl = "https://localhost:7101" })
        );
    }

    [Fact]
    public async Task WhenHandleAsync_AndEmailAlreadyExists_ThenReturnSucceeded()
    {
        // Arrange
        _authenticationService
            .Setup(x => x.IsTeacherFromPublicCegep(It.IsAny<User>()))
            .Returns(true);
        _userRepository
            .Setup(x => x.UserWithEmailExists(It.IsAny<string>()))
            .Returns(true);

        var request = new RegisterRequest
        {
            Email = "existing@cegepgarneau.ca",
            Password = "Test@1234",
            ConfirmEmailRelativeUrl = "/confirm-email"
        };

        // Act
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Assert
        _endpoint.Response.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task WhenHandleAsync_AndEmailAlreadyExists_ThenNoConfirmationEmailSent()
    {
        // Arrange
        _authenticationService
            .Setup(x => x.IsTeacherFromPublicCegep(It.IsAny<User>()))
            .Returns(true);
        _userRepository
            .Setup(x => x.UserWithEmailExists(It.IsAny<string>()))
            .Returns(true);

        var request = new RegisterRequest
        {
            Email = "existing@cegepgarneau.ca",
            Password = "Test@1234",
            ConfirmEmailRelativeUrl = "/confirm-email"
        };

        // Act
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Assert
        _notificationService.Verify(
            x => x.SendRegisterConfirmationNotification(It.IsAny<User>(), It.IsAny<string>()),
            Times.Never);
    }
}
