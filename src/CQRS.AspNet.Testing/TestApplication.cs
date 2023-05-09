using CQRS.Command.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace CQRS.AspNet.Testing;

public class TestApplication<TEntryPoint> : WebApplicationFactory<TEntryPoint>, IHostBuilderConfiguration where TEntryPoint : class
{
    private readonly List<Action<IHostBuilder>> _configureHostBuilderActions = new();

    /// <summary>
    /// Used to configure the <see cref="IHostBuilder"/> before we start to create clients.
    /// </summary>
    /// <param name="configureHostBuilder">The delegate used to configure the <see cref="IHostBuilder"/>.</param>
    /// <returns>The <see cref="ConfigurableWebApplicationFactory{TEntryPoint}"/> for chaining calls.</returns>
    public TestApplication<TEntryPoint> ConfigureHostBuilder(Action<IHostBuilder> configureHostBuilder)
    {
        ((IHostBuilderConfiguration)this).AddHostBuilderConfiguration(configureHostBuilder);
        return this;
    }

    /// <inheritdoc />
    protected override IHost CreateHost(IHostBuilder builder)
    {
        foreach (var configureHostBuilderAction in _configureHostBuilderActions)
        {
            configureHostBuilderAction.Invoke(builder);
        }
        var host = base.CreateHost(builder);
        return host;
    }


    void IHostBuilderConfiguration.AddHostBuilderConfiguration(Action<IHostBuilder> configureHostBuilder)
    {
        _configureHostBuilderActions.Add(configureHostBuilder);
    }
}

public interface IHostBuilderConfiguration
{
    void AddHostBuilderConfiguration(Action<IHostBuilder> configureHostBuilder);
}
