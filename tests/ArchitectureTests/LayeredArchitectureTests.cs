using System.Collections.Generic;
using System.Linq;
using NetArchTest.Rules;
using Xunit;

namespace ECommerceApp.ArchitectureTests;

public class LayeredArchitectureTests
{
    private static readonly string[] ForbiddenControllerDependencies =
    {
        "ECommerceApp.RyanW84.Repositories",
        "Microsoft.EntityFrameworkCore"
    };

    [Fact]
    public void ControllersShouldNotDependOnRepositoriesOrData()
    {
        TestResult result = Types
            .InAssembly(typeof(ECommerceApp.RyanW84.Program).Assembly)
            .That()
            .ResideInNamespace("ECommerceApp.RyanW84.Controllers")
            .Should()
            .NotHaveDependencyOnAny(ForbiddenControllerDependencies)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result));
    }

    [Fact]
    public void ServicesShouldNotDependOnControllers()
    {
        TestResult result = Types
            .InAssembly(typeof(ECommerceApp.RyanW84.Program).Assembly)
            .That()
            .ResideInNamespace("ECommerceApp.RyanW84.Services")
            .Should()
            .NotHaveDependencyOn("ECommerceApp.RyanW84.Controllers")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result));
    }

    [Fact]
    public void RepositoriesShouldNotDependOnControllers()
    {
        TestResult result = Types
            .InAssembly(typeof(ECommerceApp.RyanW84.Program).Assembly)
            .That()
            .ResideInNamespace("ECommerceApp.RyanW84.Repositories")
            .Should()
            .NotHaveDependencyOn("ECommerceApp.RyanW84.Controllers")
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
