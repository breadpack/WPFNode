namespace WPFNode.Models.Execution;

public interface IExecutable
{
    Task ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default);
} 