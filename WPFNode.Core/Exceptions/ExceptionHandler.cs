using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WPFNode.Core.Constants;

namespace WPFNode.Core.Exceptions;

public class ExceptionHandler
{
    private readonly ILogger _logger;

    public ExceptionHandler(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<T> HandleAsync<T>(Func<Task<T>> action, string operation)
    {
        try
        {
            return await action();
        }
        catch (NodeValidationException ex)
        {
            _logger.LogWarning(
                "[{Category}] Validation failed during {Operation}: {Message}",
                LoggerCategories.Validation,
                operation,
                ex.Message);
            throw;
        }
        catch (NodeExecutionException ex)
        {
            _logger.LogError(
                ex,
                "[{Category}] Execution failed during {Operation}: {Message}",
                LoggerCategories.Execution,
                operation,
                ex.Message);
            throw;
        }
        catch (NodePluginException ex)
        {
            _logger.LogError(
                ex,
                "[{Category}] Plugin error during {Operation}: {Message}",
                LoggerCategories.Plugin,
                operation,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{Category}] Unexpected error during {Operation}: {Message}",
                LoggerCategories.Node,
                operation,
                ex.Message);
            throw new NodeException($"예상치 못한 오류가 발생했습니다: {ex.Message}", ex);
        }
    }

    public T Handle<T>(Func<T> action, string operation)
    {
        try
        {
            return action();
        }
        catch (NodeValidationException ex)
        {
            _logger.LogWarning(
                "[{Category}] Validation failed during {Operation}: {Message}",
                LoggerCategories.Validation,
                operation,
                ex.Message);
            throw;
        }
        catch (NodeExecutionException ex)
        {
            _logger.LogError(
                ex,
                "[{Category}] Execution failed during {Operation}: {Message}",
                LoggerCategories.Execution,
                operation,
                ex.Message);
            throw;
        }
        catch (NodePluginException ex)
        {
            _logger.LogError(
                ex,
                "[{Category}] Plugin error during {Operation}: {Message}",
                LoggerCategories.Plugin,
                operation,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{Category}] Unexpected error during {Operation}: {Message}",
                LoggerCategories.Node,
                operation,
                ex.Message);
            throw new NodeException($"예상치 못한 오류가 발생했습니다: {ex.Message}", ex);
        }
    }
} 