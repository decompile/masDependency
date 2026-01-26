namespace MasDependencyMap.Core.Tests.ExtractionScoring;

using FluentAssertions;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.ExtractionScoring;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class ExternalApiDetectorTests
{
    private readonly IExternalApiDetector _detector;
    private readonly Mock<ILogger<ExternalApiDetector>> _mockLogger;
    private static readonly string TestDataRoot = Path.Combine(
        Directory.GetCurrentDirectory(),
        "..", "..", "..", "TestData", "ApiExposure");

    public ExternalApiDetectorTests()
    {
        _mockLogger = new Mock<ILogger<ExternalApiDetector>>();
        _detector = new ExternalApiDetector(_mockLogger.Object);
    }

    private static ProjectNode CreateTestProject(string projectName, string projectFileName)
    {
        var testProjectPath = Path.Combine(TestDataRoot, projectName, projectFileName);
        return new ProjectNode
        {
            ProjectName = projectName,
            ProjectPath = Path.GetFullPath(testProjectPath),
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
    }

    [Fact]
    public async Task DetectAsync_ProjectWithNoApis_Returns0Score()
    {
        // Arrange
        var project = CreateTestProject("NoApiProject", "NoApiProject.csproj");

        // Act
        var metric = await _detector.DetectAsync(project);

        // Assert
        metric.EndpointCount.Should().Be(0);
        metric.NormalizedScore.Should().Be(0);
        metric.ApiTypeBreakdown.WebApiEndpoints.Should().Be(0);
        metric.ApiTypeBreakdown.WebMethodEndpoints.Should().Be(0);
        metric.ApiTypeBreakdown.WcfEndpoints.Should().Be(0);
        metric.ProjectName.Should().Be("NoApiProject");
    }

    [Fact]
    public async Task DetectAsync_ProjectWith3Endpoints_Returns33Score()
    {
        // Arrange
        var project = CreateTestProject("WebApiProject", "WebApiProject.csproj");

        // Act
        var metric = await _detector.DetectAsync(project);

        // Assert
        metric.EndpointCount.Should().Be(3);
        metric.NormalizedScore.Should().Be(33);
        metric.ApiTypeBreakdown.WebApiEndpoints.Should().Be(3);
        metric.ApiTypeBreakdown.WebMethodEndpoints.Should().Be(0);
        metric.ApiTypeBreakdown.WcfEndpoints.Should().Be(0);
    }

    [Fact]
    public async Task DetectAsync_ProjectWith10Endpoints_Returns66Score()
    {
        // Arrange
        var project = CreateTestProject("WcfProject", "WcfProject.csproj");

        // Act
        var metric = await _detector.DetectAsync(project);

        // Assert
        metric.EndpointCount.Should().Be(10);
        metric.NormalizedScore.Should().Be(66);
        metric.ApiTypeBreakdown.WebApiEndpoints.Should().Be(0);
        metric.ApiTypeBreakdown.WebMethodEndpoints.Should().Be(0);
        metric.ApiTypeBreakdown.WcfEndpoints.Should().Be(10);
    }

    [Fact]
    public async Task DetectAsync_ProjectWith20Endpoints_Returns100Score()
    {
        // Arrange
        var project = CreateTestProject("HighExposureProject", "HighExposureProject.csproj");

        // Act
        var metric = await _detector.DetectAsync(project);

        // Assert
        metric.EndpointCount.Should().Be(20);
        metric.NormalizedScore.Should().Be(100);
        metric.ApiTypeBreakdown.WebApiEndpoints.Should().Be(20);
    }

    [Fact]
    public async Task DetectAsync_WebApiControllerWithAttributes_DetectsEndpoints()
    {
        // Arrange
        var project = CreateTestProject("WebApiProject", "WebApiProject.csproj");

        // Act
        var metric = await _detector.DetectAsync(project);

        // Assert
        metric.ApiTypeBreakdown.WebApiEndpoints.Should().BeGreaterThan(0, "should detect WebAPI endpoints with [ApiController] and HTTP verb attributes");
        metric.EndpointCount.Should().Be(metric.ApiTypeBreakdown.WebApiEndpoints);
    }

    [Fact]
    public async Task DetectAsync_WcfServiceContract_DetectsOperationContracts()
    {
        // Arrange
        var project = CreateTestProject("WcfProject", "WcfProject.csproj");

        // Act
        var metric = await _detector.DetectAsync(project);

        // Assert
        metric.ApiTypeBreakdown.WcfEndpoints.Should().Be(10, "should detect 10 [OperationContract] methods in [ServiceContract] interface");
        metric.EndpointCount.Should().Be(10);
    }

    [Fact]
    public async Task DetectAsync_MixedApiTypes_CountsAllEndpoints()
    {
        // Arrange
        var project = CreateTestProject("MixedApiProject", "MixedApiProject.csproj");

        // Act
        var metric = await _detector.DetectAsync(project);

        // Assert
        metric.ApiTypeBreakdown.WebApiEndpoints.Should().Be(2, "should detect 2 WebAPI endpoints");
        metric.ApiTypeBreakdown.WcfEndpoints.Should().Be(3, "should detect 3 WCF endpoints");
        metric.EndpointCount.Should().Be(5, "total should be sum of all API types");
        metric.NormalizedScore.Should().Be(33, "5 endpoints falls in 1-5 range = score 33");
    }

    [Fact]
    public async Task DetectAsync_RoslynUnavailable_ReturnsFallbackScore0()
    {
        // Arrange - use invalid path to trigger Roslyn failure
        var invalidPath = Path.Combine(TestDataRoot, "NonExistent", "Invalid.csproj");
        var project = new ProjectNode
        {
            ProjectName = "Invalid",
            ProjectPath = invalidPath,
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        // Act
        var metric = await _detector.DetectAsync(project);

        // Assert - fallback behavior
        metric.EndpointCount.Should().Be(0, "should default to 0 endpoints on Roslyn failure");
        metric.NormalizedScore.Should().Be(0, "should default to score 0 on Roslyn failure");
        metric.ApiTypeBreakdown.WebApiEndpoints.Should().Be(0);
        metric.ApiTypeBreakdown.WebMethodEndpoints.Should().Be(0);
        metric.ApiTypeBreakdown.WcfEndpoints.Should().Be(0);

        // Verify warning was logged
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not analyze API exposure")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DetectAsync_NullProject_ThrowsArgumentNullException()
    {
        // Arrange
        ProjectNode? nullProject = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _detector.DetectAsync(nullProject!));
    }

    [Fact]
    public async Task DetectAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var project = CreateTestProject("NoApiProject", "NoApiProject.csproj");
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _detector.DetectAsync(project, cts.Token));
    }

    [Fact]
    public async Task DetectAsync_LogsInformationMessages()
    {
        // Arrange
        var project = CreateTestProject("WebApiProject", "WebApiProject.csproj");

        // Act
        await _detector.DetectAsync(project);

        // Assert - verify structured logging
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Analyzing API exposure")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Detected") && v.ToString()!.Contains("API endpoints")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
