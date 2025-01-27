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
        _logger.LogError(new Exception("This is an exception"), "This is an error message");
        _logger.LogCritical(new Exception("This is a critical exception"), "This is a critical message");
        return Task.CompletedTask;
    }
}

public record TemperatureCommand(string City, double Value);