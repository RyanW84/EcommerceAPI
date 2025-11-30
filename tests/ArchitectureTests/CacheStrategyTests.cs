using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ECommerceApp.ArchitectureTests;

public class CacheStrategyTests
{
    private static readonly IReadOnlyCollection<Type> ControllerTypes = typeof(ECommerceApp.RyanW84.Program).Assembly
        .GetTypes()
        .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
        .ToArray();

    [Fact]
    public void HttpGetEndpointsShouldDeclareResponseCache()
    {
        var missing = new List<string>();

        foreach (MethodInfo method in GetControllerActionsWithAttribute<HttpGetAttribute>())
        {
            ResponseCacheAttribute cacheAttribute = method.GetCustomAttribute<ResponseCacheAttribute>();
            if (cacheAttribute is null)
            {
                missing.Add($"{method.DeclaringType?.FullName}.{method.Name}");
            }
        }

        Assert.True(missing.Count == 0, FormatFailure("GET endpoints missing ResponseCache", missing));
    }

    [Fact]
    public void MutatingEndpointsShouldDisableCaching()
    {
        var invalid = new List<string>();

        foreach (MethodInfo method in GetMutatingControllerActions())
        {
            ResponseCacheAttribute cacheAttribute = method.GetCustomAttribute<ResponseCacheAttribute>();
            if (cacheAttribute is null || !cacheAttribute.NoStore || cacheAttribute.Location != ResponseCacheLocation.None)
            {
                invalid.Add($"{method.DeclaringType?.FullName}.{method.Name}");
            }
        }

        Assert.True(invalid.Count == 0, FormatFailure("Mutating endpoints must disable caching", invalid));
    }

    private static IEnumerable<MethodInfo> GetControllerActionsWithAttribute<TAttribute>() where TAttribute : Attribute
    {
        return ControllerTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttribute<TAttribute>() != null);
    }

    private static IEnumerable<MethodInfo> GetMutatingControllerActions()
    {
        return ControllerTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttributes()
                .Any(attr => attr is HttpPostAttribute or HttpPutAttribute or HttpDeleteAttribute or HttpPatchAttribute));
    }

    private static string FormatFailure(string title, List<string> members)
    {
        if (members.Count == 0)
        {
            return string.Empty;
        }

        return $"{title}:{Environment.NewLine}{string.Join(Environment.NewLine, members)}";
    }
}
