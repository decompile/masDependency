using System.Runtime.InteropServices;

namespace MasDependencyMap.Core.Rendering;

/// <summary>
/// Exception thrown when Graphviz is not installed or not found in the system PATH.
/// Provides platform-specific installation instructions to help users resolve the issue.
/// </summary>
public class GraphvizNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphvizNotFoundException"/> class
    /// with a formatted error message including platform-specific installation instructions.
    /// </summary>
    public GraphvizNotFoundException()
        : base(FormatErrorMessage())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphvizNotFoundException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public GraphvizNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphvizNotFoundException"/> class
    /// with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public GraphvizNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private static string FormatErrorMessage()
    {
        var platform = GetCurrentPlatform();
        var installInstructions = GetInstallInstructions(platform);

        return $@"[red]Error:[/] Graphviz not found

[dim]Reason:[/] The 'dot' executable is not in your system PATH.

[dim]Suggestion:[/] Install Graphviz:
{installInstructions}

After installation, verify with: [green]dot -V[/]
Download page: [link]https://graphviz.org/download/[/]";
    }

    private static string GetInstallInstructions(string platform)
    {
        return platform switch
        {
            "Windows" => @"  • Chocolatey: [green]choco install graphviz[/]
  • Manual: Download installer from https://graphviz.org/download/#windows",
            "Linux" => @"  • Debian/Ubuntu: [green]sudo apt install graphviz[/]
  • RedHat/CentOS: [green]sudo yum install graphviz[/]
  • Arch: [green]sudo pacman -S graphviz[/]",
            "macOS" => @"  • Homebrew: [green]brew install graphviz[/]
  • MacPorts: [green]sudo port install graphviz[/]",
            _ => @"  • Package manager: Install 'graphviz' package
  • Manual: Download from https://graphviz.org/download/"
        };
    }

    private static string GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS";
        return "Unknown";
    }
}
