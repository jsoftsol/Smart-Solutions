// SmartSolutions.Tests/Services/AuthServiceTests.cs
using FluentAssertions;
using SmartSolutions.Core.Services;
using SmartSolutions.Tests.Helpers;

namespace SmartSolutions.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsUserWithHashedPin()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());

        await svc.CreateAsync("owner", "1234");

        var all = await svc.GetAllAsync();
        all.Should().ContainSingle(u => u.Username == "owner");
        all[0].PinHash.Should().NotBe("1234");
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenUsernameAlreadyExists()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("owner", "1234");

        var act = async () => await svc.CreateAsync("owner", "9999");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsUser_WhenCredentialsMatch()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("boy1", "0000");

        var result = await svc.ValidateAsync("boy1", "0000");

        result.Should().NotBeNull();
        result!.Username.Should().Be("boy1");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_WhenPinWrong()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("boy1", "0000");

        var result = await svc.ValidateAsync("boy1", "9999");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_WhenUsernameNotFound()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());

        var result = await svc.ValidateAsync("nobody", "0000");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_WhenUserInactive()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("boy1", "0000");
        var all = await svc.GetAllAsync();
        await svc.SetActiveAsync(all[0].Id, false);

        var result = await svc.ValidateAsync("boy1", "0000");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePinAsync_AllowsLoginWithNewPin()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("owner", "0000");
        var user = (await svc.GetAllAsync())[0];

        await svc.UpdatePinAsync(user.Id, "5678");

        (await svc.ValidateAsync("owner", "5678")).Should().NotBeNull();
        (await svc.ValidateAsync("owner", "0000")).Should().BeNull();
    }

    [Fact]
    public async Task SetActiveAsync_TogglesIsActive()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("owner", "0000");
        var user = (await svc.GetAllAsync())[0];

        await svc.SetActiveAsync(user.Id, false);
        var afterDeactivate = (await svc.GetAllAsync())[0];
        afterDeactivate.IsActive.Should().BeFalse();

        await svc.SetActiveAsync(user.Id, true);
        var afterReactivate = (await svc.GetAllAsync())[0];
        afterReactivate.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AnyUserExistsAsync_ReturnsFalse_WhenEmpty()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());

        (await svc.AnyUserExistsAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task AnyUserExistsAsync_ReturnsTrue_WhenUsersExist()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("owner", "0000");

        (await svc.AnyUserExistsAsync()).Should().BeTrue();
    }
}
