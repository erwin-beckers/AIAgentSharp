using System.Diagnostics;

namespace AIAgentSharp;

/// <summary>
///     A simple console-based logger implementation that writes messages to the console.
/// </summary>
public class ConsoleLogger : ILogger
{
    /// <summary>
    ///     Logs an informational message to the console.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogInformation(string message)
    {
        Console.WriteLine($"[INFO] {message}");
        Trace.WriteLine($"[INFO] {message}");
    }

    /// <summary>
    ///     Logs a warning message to the console.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogWarning(string message)
    {
        Console.WriteLine($"[WARN] {message}");
        Trace.WriteLine($"[WARN] {message}");
    }

    /// <summary>
    ///     Logs an error message to the console.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogError(string message)
    {
        Console.WriteLine($"[ERROR] {message}");
        Trace.WriteLine($"[ERROR] {message}");
    }

    /// <summary>
    ///     Logs a debug message to the console.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogDebug(string message)
    {
        Console.WriteLine($"[DEBUG] {message}");
        Trace.WriteLine($"[DEBUG] {message}");
    }
}