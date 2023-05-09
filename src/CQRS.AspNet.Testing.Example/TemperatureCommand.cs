using CQRS.Command.Abstractions;

namespace CQRS.AspNet.Testing.Example;

public class TemperatureCommandHandler : ICommandHandler<TemperatureCommand>
{
    private readonly ILogger<TemperatureCommand> _logger;

    public TemperatureCommandHandler(ILogger<TemperatureCommand> logger) => _logger = logger;

    public Task HandleAsync(TemperatureCommand command, CancellationToken cancellationToken)
    {
        // Create a log entry for each log level        
        _logger.LogDebug("This is a debug message");
        _logger.LogInformation("This is an information message");
        _logger.LogWarning("This is a warning message");
        _logger.LogError("This is an error message", new Exception("This is an exception"));
        _logger.LogCritical("This is a critical message", new Exception("This is a critical exception"));
        return Task.CompletedTask;
    }
}

public record TemperatureCommand(string City, double Value);