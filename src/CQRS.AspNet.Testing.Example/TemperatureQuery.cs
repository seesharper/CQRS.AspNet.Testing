using CQRS.Query.Abstractions;

namespace CQRS.AspNet.Testing.Example;

public record TemperatureQuery(string City) : IQuery<TemperatureQueryResult>;

public record TemperatureQueryResult(double Value);

public class TemperatureQueryHandler : IQueryHandler<TemperatureQuery, TemperatureQueryResult>
{
    public Task<TemperatureQueryResult> HandleAsync(TemperatureQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TemperatureQueryResult(22.0));
    }
}