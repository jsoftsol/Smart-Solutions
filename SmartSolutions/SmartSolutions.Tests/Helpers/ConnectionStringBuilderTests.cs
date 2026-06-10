using FluentAssertions;
using SmartSolutions.Core.Helpers;

namespace SmartSolutions.Tests.Helpers;

public class ConnectionStringBuilderTests
{
    [Fact]
    public void Build_WindowsAuth_ProducesCorrectString()
    {
        var result = ConnectionStringBuilder.Build(
            server: @"SERVER\SQLEXPRESS",
            database: "SmartSolutions",
            windowsAuth: true);

        result.Should().Be(
            @"Data Source=SERVER\SQLEXPRESS;Initial Catalog=SmartSolutions;Integrated Security=True;Trust Server Certificate=True");
    }

    [Fact]
    public void Build_SqlAuth_IncludesCredentials()
    {
        var result = ConnectionStringBuilder.Build(
            server: "192.168.1.10",
            database: "SmartSolutions",
            windowsAuth: false,
            username: "sa",
            password: "pass123");

        result.Should().Be(
            "Data Source=192.168.1.10;Initial Catalog=SmartSolutions;User ID=sa;Password=pass123;Trust Server Certificate=True");
    }

    [Fact]
    public void Build_WindowsAuth_IgnoresCredentials()
    {
        var result = ConnectionStringBuilder.Build(
            server: "SERVER",
            database: "db",
            windowsAuth: true,
            username: "ignored",
            password: "ignored");

        result.Should().NotContain("User ID");
        result.Should().NotContain("Password");
    }

    [Fact]
    public void Build_NullServer_Throws()
    {
        var act = () => ConnectionStringBuilder.Build(
            server: null!,
            database: "SmartSolutions",
            windowsAuth: true);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("server");
    }

    [Fact]
    public void Build_EmptyDatabase_Throws()
    {
        var act = () => ConnectionStringBuilder.Build(
            server: "SERVER",
            database: "",
            windowsAuth: true);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("database");
    }

    [Fact]
    public void Build_SqlAuth_NullUsername_Throws()
    {
        var act = () => ConnectionStringBuilder.Build(
            server: "SERVER",
            database: "SmartSolutions",
            windowsAuth: false,
            username: null,
            password: "pass123");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("username");
    }

    [Fact]
    public void Build_SqlAuth_EmptyPassword_Throws()
    {
        var act = () => ConnectionStringBuilder.Build(
            server: "SERVER",
            database: "SmartSolutions",
            windowsAuth: false,
            username: "sa",
            password: "");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("password");
    }
}
