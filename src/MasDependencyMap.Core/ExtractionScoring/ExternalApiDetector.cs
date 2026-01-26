namespace MasDependencyMap.Core.ExtractionScoring;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Detects external API exposure by scanning for web service attributes using Roslyn semantic analysis.
/// Supports detection across multiple API technologies:
/// - Modern ASP.NET Core Web API and legacy ASP.NET Web API
/// - Legacy ASMX web services
/// - WCF service contracts
/// </summary>
public class ExternalApiDetector : IExternalApiDetector
{
    private readonly ILogger<ExternalApiDetector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalApiDetector"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging of API detection progress and results.</param>
    public ExternalApiDetector(ILogger<ExternalApiDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ExternalApiMetric> DetectAsync(
        ProjectNode project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        _logger.LogInformation("Analyzing API exposure for project {ProjectName}", project.ProjectName);

        MSBuildWorkspace? workspace = null;

        try
        {
            // Create workspace for Roslyn analysis
            workspace = MSBuildWorkspace.Create();

            // Load project
            var roslynProject = await workspace.OpenProjectAsync(project.ProjectPath, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // Initialize counters for different API types
            int webApiEndpoints = 0;
            int webMethodEndpoints = 0;
            int wcfEndpoints = 0;

            // Analyze each document in the project
            foreach (var document in roslynProject.Documents)
            {
                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (syntaxTree == null)
                    continue;

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (semanticModel == null)
                    continue;

                var root = await syntaxTree.GetRootAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Detect Modern Web API endpoints
                webApiEndpoints += DetectWebApiEndpoints(root, semanticModel);

                // Detect legacy ASMX WebMethod endpoints
                webMethodEndpoints += DetectWebMethodEndpoints(root, semanticModel);

                // Detect WCF service contract endpoints
                wcfEndpoints += DetectWcfEndpoints(root, semanticModel);
            }

            // Calculate total and score
            int totalEndpoints = webApiEndpoints + webMethodEndpoints + wcfEndpoints;
            double normalizedScore = CalculateApiExposureScore(totalEndpoints);

            _logger.LogInformation(
                "Detected {TotalEndpoints} API endpoints in {ProjectName}: {WebApiCount} WebAPI, {WebMethodCount} WebMethod, {WcfCount} WCF",
                totalEndpoints, project.ProjectName, webApiEndpoints, webMethodEndpoints, wcfEndpoints);

            _logger.LogDebug(
                "Project {ProjectName}: Endpoints={EndpointCount}, Score={NormalizedScore}",
                project.ProjectName, totalEndpoints, normalizedScore);

            return new ExternalApiMetric(
                project.ProjectName,
                project.ProjectPath,
                totalEndpoints,
                normalizedScore,
                new ApiTypeBreakdown(webApiEndpoints, webMethodEndpoints, wcfEndpoints));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fallback: Roslyn unavailable - assume no external APIs (conservative)
            _logger.LogWarning(
                "Could not analyze API exposure for {ProjectName}, defaulting to 0: {Reason}",
                project.ProjectName, ex.Message);

            return new ExternalApiMetric(
                project.ProjectName,
                project.ProjectPath,
                EndpointCount: 0,
                NormalizedScore: 0,
                ApiTypeBreakdown: new ApiTypeBreakdown(0, 0, 0));
        }
        finally
        {
            // CRITICAL: Dispose workspace to prevent memory leaks
            workspace?.Dispose();
        }
    }

    /// <summary>
    /// Detects Modern Web API endpoints (ASP.NET Core Web API or legacy ASP.NET Web API).
    /// Looks for classes with [ApiController] attribute or inheriting from ApiController/ControllerBase,
    /// then counts methods with HTTP verb attributes ([HttpGet], [HttpPost], etc.).
    /// </summary>
    private int DetectWebApiEndpoints(SyntaxNode root, SemanticModel semanticModel)
    {
        int count = 0;

        // Find all class declarations
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        foreach (var classSyntax in classes)
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(classSyntax) as INamedTypeSymbol;
            if (classSymbol == null)
                continue;

            // Check if class has [ApiController] attribute
            var hasApiController = classSymbol.GetAttributes()
                .Any(attr => attr.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Mvc.ApiControllerAttribute");

            // Check if class inherits from ApiController or ControllerBase
            var inheritsApiController = IsApiControllerType(classSymbol.BaseType);

            // If this is an API controller, count methods with HTTP verb attributes
            if (hasApiController || inheritsApiController)
            {
                foreach (var member in classSymbol.GetMembers().OfType<IMethodSymbol>())
                {
                    // Check if method has HTTP verb attributes
                    var hasHttpVerb = member.GetAttributes().Any(attr =>
                        IsHttpVerbAttribute(attr.AttributeClass?.ToDisplayString()));

                    if (hasHttpVerb)
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Checks if the type is ApiController or ControllerBase (supports both ASP.NET Core and legacy ASP.NET Web API).
    /// </summary>
    private bool IsApiControllerType(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return false;

        var typeName = typeSymbol.ToDisplayString();

        return typeName == "Microsoft.AspNetCore.Mvc.ApiController" ||
               typeName == "Microsoft.AspNetCore.Mvc.ControllerBase" ||
               typeName == "System.Web.Http.ApiController";
    }

    /// <summary>
    /// Checks if the attribute is an HTTP verb attribute (supports both ASP.NET Core and legacy ASP.NET Web API).
    /// </summary>
    private bool IsHttpVerbAttribute(string? attributeName)
    {
        if (attributeName == null)
            return false;

        // ASP.NET Core Web API attributes
        if (attributeName == "Microsoft.AspNetCore.Mvc.HttpGetAttribute" ||
            attributeName == "Microsoft.AspNetCore.Mvc.HttpPostAttribute" ||
            attributeName == "Microsoft.AspNetCore.Mvc.HttpPutAttribute" ||
            attributeName == "Microsoft.AspNetCore.Mvc.HttpDeleteAttribute" ||
            attributeName == "Microsoft.AspNetCore.Mvc.HttpPatchAttribute" ||
            attributeName == "Microsoft.AspNetCore.Mvc.RouteAttribute")
        {
            return true;
        }

        // Legacy ASP.NET Web API attributes (System.Web.Http)
        if (attributeName == "System.Web.Http.HttpGetAttribute" ||
            attributeName == "System.Web.Http.HttpPostAttribute" ||
            attributeName == "System.Web.Http.HttpPutAttribute" ||
            attributeName == "System.Web.Http.HttpDeleteAttribute" ||
            attributeName == "System.Web.Http.RouteAttribute")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Detects legacy ASMX WebMethod endpoints.
    /// Looks for methods with [WebMethod] attribute from System.Web.Services.
    /// </summary>
    private int DetectWebMethodEndpoints(SyntaxNode root, SemanticModel semanticModel)
    {
        int count = 0;

        // Find all method declarations
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var methodSyntax in methods)
        {
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax);
            if (methodSymbol == null)
                continue;

            // Check if method has [WebMethod] attribute
            var hasWebMethod = methodSymbol.GetAttributes()
                .Any(attr => attr.AttributeClass?.ToDisplayString() == "System.Web.Services.WebMethodAttribute");

            if (hasWebMethod)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Detects WCF service contract endpoints.
    /// Looks for interfaces with [ServiceContract] attribute,
    /// then counts methods with [OperationContract] attribute.
    /// </summary>
    private int DetectWcfEndpoints(SyntaxNode root, SemanticModel semanticModel)
    {
        int count = 0;

        // Find all interface declarations
        var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();

        foreach (var interfaceSyntax in interfaces)
        {
            var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceSyntax) as INamedTypeSymbol;
            if (interfaceSymbol == null)
                continue;

            // Check if interface has [ServiceContract] attribute
            var hasServiceContract = interfaceSymbol.GetAttributes()
                .Any(attr => attr.AttributeClass?.ToDisplayString() == "System.ServiceModel.ServiceContractAttribute");

            // If this is a service contract, count methods with [OperationContract]
            if (hasServiceContract)
            {
                foreach (var member in interfaceSymbol.GetMembers().OfType<IMethodSymbol>())
                {
                    var hasOperationContract = member.GetAttributes()
                        .Any(attr => attr.AttributeClass?.ToDisplayString() == "System.ServiceModel.OperationContractAttribute");

                    if (hasOperationContract)
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Calculates API exposure score using stepped thresholds.
    /// Scoring is NOT linear - uses discrete categories to match stakeholder mental model.
    /// </summary>
    /// <param name="totalEndpoints">Total count of API endpoints detected.</param>
    /// <returns>
    /// Normalized score in 0-100 range:
    /// - 0 endpoints = 0 (no external APIs, easy to extract)
    /// - 1-5 endpoints = 33 (low exposure, manageable API surface)
    /// - 6-15 endpoints = 66 (medium exposure, significant API surface)
    /// - 16+ endpoints = 100 (high exposure, complex API surface)
    /// </returns>
    private static double CalculateApiExposureScore(int totalEndpoints)
    {
        // Stepped scoring based on acceptance criteria
        if (totalEndpoints == 0)
            return 0;   // No external APIs
        else if (totalEndpoints <= 5)
            return 33;  // Low exposure (1-5 endpoints)
        else if (totalEndpoints <= 15)
            return 66;  // Medium exposure (6-15 endpoints)
        else
            return 100; // High exposure (16+ endpoints)

        // Note: NOT linear interpolation - discrete thresholds per acceptance criteria
    }
}
