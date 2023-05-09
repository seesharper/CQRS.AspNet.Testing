using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace CQRS.AspNet.Testing;

/// <summary>
/// Represents a "configurable" <see cref="WebApplicationFactory{TEntryPoint}"/> that allows us to configure the <see cref="IHostBuilder"/> before we start to create clients.
/// </summary>
/// <typeparam name="TEntryPoint">A type in the entry point assembly of the application. This is usually the Program class</typeparam>
public class TestApplication<TEntryPoint> : WebApplicationFactory<TEntryPoint>, IHostBuilderConfiguration where TEntryPoint : class
{
    private readonly List<Action<IHostBuilder>> _configureHostBuilderActions = new();

    /// <summary>
    /// Used to configure the <see cref="IHostBuilder"/> before we start to create clients.
    /// </summary>
    /// <param name="configureHostBuilder">The delegate used to configure the <see cref="IHostBuilder"/>.</param>
    /// <returns>The <see cref="TestApplication{TEntryPoint}"/> for chaining calls.</returns>
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

/// <summary>
/// Represents the configuration of the <see cref="IHostBuilder"/>.
/// </summary>
public interface IHostBuilderConfiguration
{
    /// <summary>
    /// Adds an action to configure the <see cref="IHostBuilder"/>.
    /// </summary>
    /// <param name="configureHostBuilder"></param>
    void AddHostBuilderConfiguration(Action<IHostBuilder> configureHostBuilder);
}
