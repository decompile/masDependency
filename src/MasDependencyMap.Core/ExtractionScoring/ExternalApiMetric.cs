namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Breakdown of API endpoint counts by technology type.
/// Provides detailed insight into which API technologies are used in a project.
/// </summary>
/// <param name="WebApiEndpoints">Count of modern ASP.NET Core Web API or legacy ASP.NET Web API endpoints (methods with HTTP verb attributes like [HttpGet], [HttpPost]).</param>
/// <param name="WebMethodEndpoints">Count of legacy ASMX web service methods ([WebMethod] attributes from System.Web.Services).</param>
/// <param name="WcfEndpoints">Count of WCF service operation contracts ([OperationContract] attributes from System.ServiceModel).</param>
public sealed record ApiTypeBreakdown(
    int WebApiEndpoints,
    int WebMethodEndpoints,
    int WcfEndpoints);

/// <summary>
/// Represents external API exposure metrics for a single project.
/// API exposure quantifies extraction difficulty based on public API surface area and external consumer dependencies.
/// More external APIs indicate harder extraction due to:
/// - Breaking changes: Each API is a contract with external consumers
/// - Testing complexity: Each endpoint requires integration and contract tests
/// - Migration coordination: External consumers must be updated or API versioning needed
/// - Security surface: More endpoints = larger attack surface to secure
/// - Documentation burden: Each API needs documentation for external consumers
/// </summary>
/// <param name="ProjectName">Name of the project being analyzed.</param>
/// <param name="ProjectPath">Absolute path to the project file (.csproj or .vbproj).</param>
/// <param name="EndpointCount">Total count of external API endpoints detected (sum of all API types: WebAPI + WebMethod + WCF).</param>
/// <param name="NormalizedScore">
/// API exposure score normalized to 0-100 scale using stepped thresholds:
/// - 0 endpoints = 0 (no APIs, easy to extract)
/// - 1-5 endpoints = 33 (low exposure, manageable API surface)
/// - 6-15 endpoints = 66 (medium exposure, significant API surface)
/// - 16+ endpoints = 100 (high exposure, complex API surface)
/// Higher scores indicate harder extraction.
/// </param>
/// <param name="ApiTypeBreakdown">Detailed breakdown of endpoint counts by API technology type (WebAPI, WebMethod, WCF). Useful for understanding API technology mix.</param>
public sealed record ExternalApiMetric(
    string ProjectName,
    string ProjectPath,
    int EndpointCount,
    double NormalizedScore,
    ApiTypeBreakdown ApiTypeBreakdown);
