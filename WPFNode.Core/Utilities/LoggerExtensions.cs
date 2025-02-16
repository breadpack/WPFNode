using Microsoft.Extensions.Logging;
using System;
using WPFNode.Core.Constants;
using WPFNode.Core.Models;

namespace WPFNode.Core.Utilities;

public static class LoggerExtensions
{
    public static void LogNodeExecution(this ILogger logger, NodeBase node, string message)
    {
        logger.LogInformation(
            "[{Category}] {NodeId}:{NodeName} - {Message}",
            LoggerCategories.Execution,
            node.Id,
            node.Name,
            message);
    }

    public static void LogNodeError(this ILogger logger, NodeBase node, Exception ex, string message)
    {
        logger.LogError(
            ex,
            "[{Category}] {NodeId}:{NodeName} - {Message}",
            LoggerCategories.Node,
            node.Id,
            node.Name,
            message);
    }

    public static void LogPerformance(this ILogger logger, string operation, TimeSpan duration)
    {
        logger.LogInformation(
            "[{Category}] {Operation} completed in {Duration:N0}ms",
            LoggerCategories.Performance,
            operation,
            duration.TotalMilliseconds);
    }

    public static void LogValidation(this ILogger logger, string message, params object[] args)
    {
        logger.LogWarning(
            "[{Category}] {Message}",
            LoggerCategories.Validation,
            string.Format(message, args));
    }

    public static void LogSecurity(this ILogger logger, string message, params object[] args)
    {
        logger.LogWarning(
            "[{Category}] {Message}",
            LoggerCategories.Security,
            string.Format(message, args));
    }
} 