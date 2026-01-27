namespace MasDependencyMap.Core.Tests.Reporting;

using System.Diagnostics;
using System.Globalization;
using System.Text;
using CsvHelper;
using FluentAssertions;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.ExtractionScoring;
using MasDependencyMap.Core.Reporting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public sealed class CsvExporterTests : IDisposable
{
    private readonly CsvExporter _exporter;
    private readonly Mock<ILogger<CsvExporter>> _loggerMock;
    private readonly List<string> _tempDirectories;

    public CsvExporterTests()
    {
        _loggerMock = new Mock<ILogger<CsvExporter>>();
        _exporter = new CsvExporter(_loggerMock.Object);
        _tempDirectories = new List<string>();
    }

    public void Dispose()
    {
        // Cleanup all temp directories created during tests
        foreach (var dir in _tempDirectories)
        {
            CleanupTempDirectory(dir);
        }
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_ValidScores_GeneratesValidCsv()
    {
        // Arrange
        var scores = CreateTestExtractionScores(count: 20);
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportExtractionScoresAsync(
            scores, outputDir, "TestSolution");

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("Project Name");
        content.Should().Contain("Extraction Score");

        // Parse CSV and verify row count
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<ExtractionScoreRecordMap>();
        var records = csv.GetRecords<ExtractionScoreRecord>().ToList();
        records.Should().HaveCount(20);
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_SortsByExtractionScore_Ascending()
    {
        // Arrange
        var scores = new List<ExtractionScore>
        {
            CreateExtractionScore("ProjectA", finalScore: 75.5),
            CreateExtractionScore("ProjectB", finalScore: 23.2),
            CreateExtractionScore("ProjectC", finalScore: 45.8)
        };
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportExtractionScoresAsync(
            scores, outputDir, "TestSolution");

        // Assert
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<ExtractionScoreRecordMap>();
        var records = csv.GetRecords<ExtractionScoreRecord>().ToList();

        // Verify ascending order
        records[0].ProjectName.Should().Be("ProjectB");  // 23.2
        records[1].ProjectName.Should().Be("ProjectC");  // 45.8
        records[2].ProjectName.Should().Be("ProjectA");  // 75.5
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_ColumnHeaders_UseTitleCaseWithSpaces()
    {
        // Arrange
        var scores = CreateTestExtractionScores(count: 1);
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportExtractionScoresAsync(
            scores, outputDir, "TestSolution");

        // Assert
        var lines = await File.ReadAllLinesAsync(filePath);
        var headerLine = lines[0];

        headerLine.Should().Contain("Project Name");
        headerLine.Should().Contain("Extraction Score");
        headerLine.Should().Contain("Coupling Metric");
        headerLine.Should().Contain("Complexity Metric");
        headerLine.Should().Contain("Tech Debt Score");
        headerLine.Should().Contain("External APIs");

        // Verify exact order
        var expectedHeader = "Project Name,Extraction Score,Coupling Metric,Complexity Metric,Tech Debt Score,External APIs";
        headerLine.Should().Be(expectedHeader);
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_WithNullCouplingMetric_ExportsNA()
    {
        // Arrange
        var score = new ExtractionScore(
            "ProjectA",
            "path",
            50.0,
            null,  // Explicitly null CouplingMetric
            new ComplexityMetric("ProjectA", "path", 50, 150, 3.0, 25.5),
            new TechDebtMetric("ProjectA", "path", "net8.0", 10.0),
            new ExternalApiMetric("ProjectA", "path", 2, 20.0, new ApiTypeBreakdown(1, 1, 0)));

        var scores = new List<ExtractionScore> { score };
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportExtractionScoresAsync(
            scores, outputDir, "TestSolution");

        // Assert
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<ExtractionScoreRecordMap>();
        var records = csv.GetRecords<ExtractionScoreRecord>().ToList();

        records[0].CouplingMetric.Should().Be("N/A");

        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("null CouplingMetric")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_UTF8WithBOM_HasCorrectBOM()
    {
        // Arrange
        var scores = CreateTestExtractionScores(count: 5);
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportExtractionScoresAsync(
            scores, outputDir, "TestSolution");

        // Assert - Check for UTF-8 BOM
        var bytes = await File.ReadAllBytesAsync(filePath);
        bytes.Should().HaveCountGreaterThan(3);

        // UTF-8 BOM: 0xEF, 0xBB, 0xBF
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_EmptyScores_CreatesEmptyCsv()
    {
        // Arrange
        var scores = new List<ExtractionScore>();
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportExtractionScoresAsync(
            scores, outputDir, "TestSolution");

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var lines = await File.ReadAllLinesAsync(filePath);
        lines.Should().HaveCount(1);  // Header only
        lines[0].Should().Contain("Project Name");
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_SingleScore_ExportsCorrectly()
    {
        // Arrange
        var score = CreateExtractionScore("SingleProject", finalScore: 42.5);
        var scores = new List<ExtractionScore> { score };
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportExtractionScoresAsync(
            scores, outputDir, "TestSolution");

        // Assert
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<ExtractionScoreRecordMap>();
        var records = csv.GetRecords<ExtractionScoreRecord>().ToList();

        records.Should().HaveCount(1);
        records[0].ProjectName.Should().Be("SingleProject");
        records[0].ExtractionScore.Should().Be("42.5");  // 1 decimal place
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_LargeDataset_CompletesWithin10Seconds()
    {
        // Arrange
        var scores = CreateTestExtractionScores(count: 1000);  // 1000 projects
        var outputDir = CreateTempDirectory();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var filePath = await _exporter.ExportExtractionScoresAsync(
            scores, outputDir, "LargeSolution");
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000);  // 10 seconds

        // Verify file integrity
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<ExtractionScoreRecordMap>();
        var records = csv.GetRecords<ExtractionScoreRecord>().ToList();
        records.Should().HaveCount(1000);
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_CancellationToken_CancelsOperation()
    {
        // Arrange
        var scores = CreateTestExtractionScores(count: 100);
        var outputDir = CreateTempDirectory();
        var cts = new CancellationTokenSource();
        cts.Cancel();  // Cancel immediately

        // Act & Assert
        // CsvHelper wraps OperationCanceledException in WriterException
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _exporter.ExportExtractionScoresAsync(
                scores, outputDir, "TestSolution", cts.Token));

        // Verify inner exception is OperationCanceledException
        var innerEx = ex;
        while (innerEx.InnerException != null)
        {
            innerEx = innerEx.InnerException;
        }
        innerEx.Should().BeOfType<OperationCanceledException>();
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_NullScores_ThrowsArgumentNullException()
    {
        // Arrange
        var outputDir = CreateTempDirectory();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _exporter.ExportExtractionScoresAsync(
                null!, outputDir, "TestSolution"));
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_EmptyOutputDirectory_ThrowsArgumentException()
    {
        // Arrange
        var scores = CreateTestExtractionScores(count: 1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _exporter.ExportExtractionScoresAsync(
                scores, string.Empty, "TestSolution"));
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_EmptySolutionName_ThrowsArgumentException()
    {
        // Arrange
        var scores = CreateTestExtractionScores(count: 1);
        var outputDir = CreateTempDirectory();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _exporter.ExportExtractionScoresAsync(
                scores, outputDir, string.Empty));
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_InvalidFileNameCharacters_SanitizesFileName()
    {
        // Arrange
        var scores = CreateTestExtractionScores(count: 1);
        var outputDir = CreateTempDirectory();
        var solutionNameWithInvalidChars = "Test<Solution>With:Invalid*Chars";

        // Act
        var filePath = await _exporter.ExportExtractionScoresAsync(
            scores, outputDir, solutionNameWithInvalidChars);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        Path.GetFileName(filePath).Should().Contain("_");  // Invalid chars replaced with underscore
        Path.GetFileName(filePath).Should().NotContain("<");
        Path.GetFileName(filePath).Should().NotContain(">");
        Path.GetFileName(filePath).Should().NotContain(":");
        Path.GetFileName(filePath).Should().NotContain("*");
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_DecimalFormatting_UsesOneDecimalPlace()
    {
        // Arrange
        var score = CreateExtractionScore("TestProject", finalScore: 42.789);
        var scores = new List<ExtractionScore> { score };
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportExtractionScoresAsync(
            scores, outputDir, "TestSolution");

        // Assert
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<ExtractionScoreRecordMap>();
        var records = csv.GetRecords<ExtractionScoreRecord>().ToList();

        // Verify all scores formatted with 1 decimal place
        records[0].ExtractionScore.Should().Be("42.8");  // Rounded from 42.789
        records[0].CouplingMetric.Should().MatchRegex(@"^\d+\.\d$");  // Pattern: digit.digit
        records[0].ComplexityMetric.Should().MatchRegex(@"^\d+\.\d$");
        records[0].TechDebtScore.Should().MatchRegex(@"^\d+\.\d$");
    }

    [Fact]
    public async Task ExportExtractionScoresAsync_ExternalAPIsColumn_IsInteger()
    {
        // Arrange
        var score = CreateExtractionScore("TestProject", finalScore: 50.0);
        var scores = new List<ExtractionScore> { score };
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportExtractionScoresAsync(
            scores, outputDir, "TestSolution");

        // Assert
        var content = await File.ReadAllTextAsync(filePath);
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // Check data row (not header) - external APIs should be integer with no decimal
        lines[1].Should().EndWith(",2");  // External API count from CreateExtractionScore
        lines[1].Should().NotContain("2.0");  // Should NOT have decimal point
    }

    // ===== Story 5.6: Cycle Analysis CSV Export Tests =====

    [Fact]
    public async Task ExportCycleAnalysisAsync_ValidCycles_GeneratesValidCsv()
    {
        // Arrange
        var cycles = CreateTestCycles(count: 5);
        var suggestions = CreateTestSuggestions(cycles);
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportCycleAnalysisAsync(
            cycles, suggestions, outputDir, "TestSolution");

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("Cycle ID");
        content.Should().Contain("Cycle Size");
        content.Should().Contain("Projects Involved");

        // Parse CSV and verify row count
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CycleAnalysisRecordMap>();
        var records = csv.GetRecords<CycleAnalysisRecord>().ToList();
        records.Should().HaveCount(5);
    }

    [Fact]
    public async Task ExportCycleAnalysisAsync_SortsByCycleId_Ascending()
    {
        // Arrange
        var cycles = new List<CycleInfo>
        {
            CreateCycle(cycleId: 3, projectCount: 2),
            CreateCycle(cycleId: 1, projectCount: 5),
            CreateCycle(cycleId: 2, projectCount: 3)
        };
        var suggestions = CreateTestSuggestions(cycles);
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportCycleAnalysisAsync(
            cycles, suggestions, outputDir, "TestSolution");

        // Assert
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CycleAnalysisRecordMap>();
        var records = csv.GetRecords<CycleAnalysisRecord>().ToList();

        // Verify ascending order by CycleId
        records[0].CycleId.Should().Be(1);
        records[1].CycleId.Should().Be(2);
        records[2].CycleId.Should().Be(3);
    }

    [Fact]
    public async Task ExportCycleAnalysisAsync_ColumnHeaders_UseTitleCaseWithSpaces()
    {
        // Arrange
        var cycles = CreateTestCycles(count: 1);
        var suggestions = CreateTestSuggestions(cycles);
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportCycleAnalysisAsync(
            cycles, suggestions, outputDir, "TestSolution");

        // Assert
        var lines = await File.ReadAllLinesAsync(filePath);
        var headerLine = lines[0];

        headerLine.Should().Contain("Cycle ID");
        headerLine.Should().Contain("Cycle Size");
        headerLine.Should().Contain("Projects Involved");
        headerLine.Should().Contain("Suggested Break Point");
        headerLine.Should().Contain("Coupling Score");

        // Verify exact order
        var expectedHeader = "Cycle ID,Cycle Size,Projects Involved,Suggested Break Point,Coupling Score";
        headerLine.Should().Be(expectedHeader);
    }

    [Fact]
    public async Task ExportCycleAnalysisAsync_ProjectsInvolved_CommaSeparated()
    {
        // Arrange
        var projects = new List<ProjectNode>
        {
            new() { ProjectName = "ProjectA", ProjectPath = "pathA", TargetFramework = "net8.0", SolutionName = "TestSolution" },
            new() { ProjectName = "ProjectB", ProjectPath = "pathB", TargetFramework = "net8.0", SolutionName = "TestSolution" },
            new() { ProjectName = "ProjectC", ProjectPath = "pathC", TargetFramework = "net8.0", SolutionName = "TestSolution" }
        };
        var cycle = new CycleInfo(1, projects);
        var suggestion = CreateSuggestion(cycleId: 1, projects[0], projects[1], couplingScore: 5, cycleSize: 3, rank: 1);
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportCycleAnalysisAsync(
            new[] { cycle }, new[] { suggestion }, outputDir, "TestSolution");

        // Assert
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CycleAnalysisRecordMap>();
        var records = csv.GetRecords<CycleAnalysisRecord>().ToList();

        records[0].ProjectsInvolved.Should().Be("ProjectA, ProjectB, ProjectC");
    }

    [Fact]
    public async Task ExportCycleAnalysisAsync_SuggestedBreakPoint_FormattedCorrectly()
    {
        // Arrange
        var projects = CreateTestProjects(3);
        var cycle = new CycleInfo(1, projects);
        var suggestion = CreateSuggestion(cycleId: 1, projects[0], projects[1], couplingScore: 5, cycleSize: 3, rank: 1);
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportCycleAnalysisAsync(
            new[] { cycle }, new[] { suggestion }, outputDir, "TestSolution");

        // Assert
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CycleAnalysisRecordMap>();
        var records = csv.GetRecords<CycleAnalysisRecord>().ToList();

        // Verify arrow format: "Source → Target"
        records[0].SuggestedBreakPoint.Should().Be($"{projects[0].ProjectName} → {projects[1].ProjectName}");
        records[0].SuggestedBreakPoint.Should().Contain("→");
    }

    [Fact]
    public async Task ExportCycleAnalysisAsync_CycleWithoutSuggestion_ExportsNA()
    {
        // Arrange
        var cycle = CreateCycle(cycleId: 1, projectCount: 2);
        var suggestions = new List<CycleBreakingSuggestion>(); // Empty - no suggestions
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportCycleAnalysisAsync(
            new[] { cycle }, suggestions, outputDir, "TestSolution");

        // Assert
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CycleAnalysisRecordMap>();
        var records = csv.GetRecords<CycleAnalysisRecord>().ToList();

        records[0].SuggestedBreakPoint.Should().Be("N/A");
        records[0].CouplingScore.Should().Be(0);

        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("no breaking suggestion")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportCycleAnalysisAsync_UTF8WithBOM_OpensInExcel()
    {
        // Arrange
        var cycles = CreateTestCycles(count: 3);
        var suggestions = CreateTestSuggestions(cycles);
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportCycleAnalysisAsync(
            cycles, suggestions, outputDir, "TestSolution");

        // Assert - Check for UTF-8 BOM
        var bytes = await File.ReadAllBytesAsync(filePath);
        bytes.Should().HaveCountGreaterThan(3);

        // UTF-8 BOM: 0xEF, 0xBB, 0xBF
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);
    }

    [Fact]
    public async Task ExportCycleAnalysisAsync_EmptyCycles_CreatesEmptyCsv()
    {
        // Arrange
        var cycles = new List<CycleInfo>();
        var suggestions = new List<CycleBreakingSuggestion>();
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportCycleAnalysisAsync(
            cycles, suggestions, outputDir, "TestSolution");

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var lines = await File.ReadAllLinesAsync(filePath);
        lines.Should().HaveCount(1);  // Header only
        lines[0].Should().Contain("Cycle ID");
    }

    [Fact]
    public async Task ExportCycleAnalysisAsync_LargeCycle_HandlesLongProjectList()
    {
        // Arrange
        var projects = CreateTestProjects(20);  // 20 projects in one cycle
        var cycle = new CycleInfo(1, projects);
        var suggestion = CreateSuggestion(cycleId: 1, projects[0], projects[1], couplingScore: 5, cycleSize: 20, rank: 1);
        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportCycleAnalysisAsync(
            new[] { cycle }, new[] { suggestion }, outputDir, "TestSolution");

        // Assert
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CycleAnalysisRecordMap>();
        var records = csv.GetRecords<CycleAnalysisRecord>().ToList();

        // Verify all 20 projects are in the list
        var projectNames = records[0].ProjectsInvolved.Split(", ");
        projectNames.Should().HaveCount(20);

        // Verify RFC 4180 quoting (field should be quoted because it contains commas)
        var rawContent = await File.ReadAllTextAsync(filePath);
        rawContent.Should().Contain("\"Project0, Project1,");  // Quoted field
    }

    [Fact]
    public async Task ExportCycleAnalysisAsync_CancellationToken_CancelsOperation()
    {
        // Arrange
        var cycles = CreateTestCycles(count: 100);
        var suggestions = CreateTestSuggestions(cycles);
        var outputDir = CreateTempDirectory();
        var cts = new CancellationTokenSource();
        cts.Cancel();  // Cancel immediately

        // Act & Assert
        // CsvHelper wraps OperationCanceledException in WriterException
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _exporter.ExportCycleAnalysisAsync(
                cycles, suggestions, outputDir, "TestSolution", cts.Token));

        // Verify inner exception is OperationCanceledException
        var innerEx = ex;
        while (innerEx.InnerException != null)
        {
            innerEx = innerEx.InnerException;
        }
        innerEx.Should().BeOfType<OperationCanceledException>();
    }

    [Fact]
    public async Task ExportCycleAnalysisAsync_MultipleSuggestionsPerCycle_UsesLowestRank()
    {
        // Arrange: Create cycle with multiple suggestions at different ranks
        var projects = CreateTestProjects(3);
        var cycle = new CycleInfo(1, projects);

        var suggestions = new List<CycleBreakingSuggestion>
        {
            // Rank 3 (should NOT be selected)
            CreateSuggestion(cycleId: 1, projects[0], projects[2], couplingScore: 15, cycleSize: 3, rank: 3),
            // Rank 1 (should be selected - lowest rank)
            CreateSuggestion(cycleId: 1, projects[1], projects[2], couplingScore: 5, cycleSize: 3, rank: 1),
            // Rank 2 (should NOT be selected)
            CreateSuggestion(cycleId: 1, projects[0], projects[1], couplingScore: 10, cycleSize: 3, rank: 2)
        };

        var outputDir = CreateTempDirectory();

        // Act
        var filePath = await _exporter.ExportCycleAnalysisAsync(
            new[] { cycle }, suggestions, outputDir, "TestSolution");

        // Assert
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CycleAnalysisRecordMap>();
        var records = csv.GetRecords<CycleAnalysisRecord>().ToList();

        // Verify the suggestion with Rank=1 was selected (Project1 → Project2, coupling=5)
        records[0].SuggestedBreakPoint.Should().Be($"{projects[1].ProjectName} → {projects[2].ProjectName}");
        records[0].CouplingScore.Should().Be(5);
    }

    // ===== Helper Methods =====

    // Helper: Create test extraction scores
    private IReadOnlyList<ExtractionScore> CreateTestExtractionScores(int count)
    {
        var scores = new List<ExtractionScore>();
        var random = new Random(42);  // Fixed seed for reproducibility

        for (int i = 0; i < count; i++)
        {
            scores.Add(CreateExtractionScore($"Project{i}", random.Next(0, 100)));
        }

        return scores;
    }

    private ExtractionScore CreateExtractionScore(
        string projectName,
        double finalScore,
        CouplingMetric? couplingMetric = null)
    {
        var coupling = couplingMetric ?? new CouplingMetric(projectName, 3, 2, 8, 15.2);
        var complexity = new ComplexityMetric(projectName, "path", 50, 150, 3.0, 25.5);
        var techDebt = new TechDebtMetric(projectName, "path", "net8.0", 10.0);
        var externalApi = new ExternalApiMetric(
            projectName,
            "path",
            2,
            20.0,
            new ApiTypeBreakdown(1, 1, 0));

        return new ExtractionScore(
            projectName,
            "path",
            finalScore,
            coupling,
            complexity,
            techDebt,
            externalApi);
    }

    private string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);
        return tempDir;
    }

    private static void CleanupTempDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch
            {
                // Best effort cleanup - ignore failures
            }
        }
    }

    // Helper: Create test cycles
    private IReadOnlyList<CycleInfo> CreateTestCycles(int count)
    {
        var cycles = new List<CycleInfo>();
        for (int i = 0; i < count; i++)
        {
            var projects = CreateTestProjects(3 + i % 5);  // Varying cycle sizes
            cycles.Add(new CycleInfo(i + 1, projects));
        }
        return cycles;
    }

    private CycleInfo CreateCycle(int cycleId, int projectCount)
    {
        var projects = CreateTestProjects(projectCount);
        return new CycleInfo(cycleId, projects);
    }

    private IReadOnlyList<ProjectNode> CreateTestProjects(int count)
    {
        var projects = new List<ProjectNode>();
        for (int i = 0; i < count; i++)
        {
            projects.Add(new ProjectNode
            {
                ProjectName = $"Project{i}",
                ProjectPath = $"path{i}",
                TargetFramework = "net8.0",
                SolutionName = "TestSolution"
            });
        }
        return projects;
    }

    // Helper: Create test suggestions
    private IReadOnlyList<CycleBreakingSuggestion> CreateTestSuggestions(IReadOnlyList<CycleInfo> cycles)
    {
        var suggestions = new List<CycleBreakingSuggestion>();
        foreach (var cycle in cycles)
        {
            if (cycle.Projects.Count >= 2)
            {
                suggestions.Add(CreateSuggestion(
                    cycle.CycleId,
                    cycle.Projects[0],
                    cycle.Projects[1],
                    couplingScore: 5,
                    cycleSize: cycle.CycleSize,
                    rank: cycle.CycleId));
            }
        }
        return suggestions;
    }

    private CycleBreakingSuggestion CreateSuggestion(
        int cycleId,
        ProjectNode source,
        ProjectNode target,
        int couplingScore,
        int cycleSize,
        int rank)
    {
        var rationale = $"Weakest link in {cycleSize}-project cycle, only {couplingScore} method calls";
        return new CycleBreakingSuggestion(cycleId, source, target, couplingScore, cycleSize, rationale)
        {
            Rank = rank
        };
    }
}
