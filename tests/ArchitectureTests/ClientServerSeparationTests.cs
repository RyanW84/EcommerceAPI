using System.Collections.Generic;
using System.Linq;
using ECommerceApp.ConsoleClient;
using ECommerceApp.RyanW84;
using NetArchTest.Rules;
using Xunit;

namespace ECommerceApp.ArchitectureTests;

public class ClientServerSeparationTests
{
    private static readonly string[] SessionDependencies =
    {
        "Microsoft.AspNetCore.Session",
        "Microsoft.AspNetCore.Http.ISession",
        "Microsoft.AspNetCore.Http.IHttpContextAccessor",
    };

    [Fact]
    public void ApiAssemblyShouldNotDependOnConsoleClient()
    {
        TestResult result = Types
            .InAssembly(typeof(Program).Assembly)
            .Should()
            .NotHaveDependencyOn("ECommerceApp.ConsoleClient")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result));
    }

    [Fact]
    public void ConsoleClientShouldNotDependOnServerInternalLayers()
    {
        var forbidden = new[]
        {
            "ECommerceApp.RyanW84.Data",
            "ECommerceApp.RyanW84.Services",
            "ECommerceApp.RyanW84.Repositories",
            "ECommerceApp.RyanW84.Controllers",
            "Microsoft.EntityFrameworkCore",
        };

        TestResult result = Types
            .InAssembly(typeof(ConsoleApp).Assembly)
            .Should()
            .NotHaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result));
    }

    [Fact]
    public void ApiControllersShouldRemainUiAgnostic()
    {
        TestResult result = Types
            .InAssembly(typeof(Program).Assembly)
            .That()
            .ResideInNamespace("ECommerceApp.RyanW84.Controllers")
            .Should()
            .NotHaveDependencyOn("Spectre.Console")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result));
    }

    [Fact]
    public void ApiAssemblyShouldRemainStateless()
    {
        TestResult result = Types
            .InAssembly(typeof(Program).Assembly)
            .Should()
            .NotHaveDependencyOnAny(SessionDependencies)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result));
    }

    private static string FormatFailure(TestResult result)
    {
        if (result.IsSuccessful)
        {
            return string.Empty;
        }

        IEnumerable<string> failing = result.FailingTypes.Select(type => type.FullName);
        return "Detected forbidden dependencies:\n" + string.Join("\n", failing);
    }
}
