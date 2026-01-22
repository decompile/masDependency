using Microsoft.Build.Locator;

namespace MasDependencyMap.Core.Tests.SolutionLoading;

/// <summary>
/// xUnit class fixture to ensure MSBuildLocator.RegisterDefaults() is called once before any tests run.
/// CRITICAL: MSBuildLocator must be registered BEFORE any Roslyn types are loaded.
/// This fixture ensures the registration happens at the start of the test run.
/// </summary>
public class MSBuildLocatorFixture : IDisposable
{
    public MSBuildLocatorFixture()
    {
        // CRITICAL: Register MSBuild location before any Roslyn usage
        // This is the same requirement as Program.Main() but for tests
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }

    public void Dispose()
    {
        // No cleanup needed for MSBuildLocator
        GC.SuppressFinalize(this);
    }
}
