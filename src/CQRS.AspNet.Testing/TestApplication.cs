using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CQRS.AspNet.Testing;

/// <summary>
/// Represents a "configurable" <see cref="WebApplicationFactory{TEntryPoint}"/> that allows us to configure the <see cref="IHostBuilder"/> before we start to create clients.
/// </summary>
/// <typeparam name="TEntryPoint">A type in the entry point assembly of the application. This is usually the Program class</typeparam>
public class TestApplication<TEntryPoint> : WebApplicationFactory<TEntryPoint>, IHostBuilderConfiguration where TEntryPoint : class
{
    private readonly List<Action<IHostBuilder>> _configureHostBuilderActions = new();
    private readonly MutableConfigurationProvider _mutableConfigurationProvider = new();

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

    internal TestApplication<TEntryPoint> UpdateConfiguration(string key, string? value)
    {
        _mutableConfigurationProvider.Update(key, value);
        return this;
    }

    /// <inheritdoc />
    protected override IHost CreateHost(IHostBuilder builder)
    {
        foreach (var configureHostBuilderAction in _configureHostBuilderActions)
        {
            configureHostBuilderAction.Invoke(builder);
        }
        // Added last so it takes precedence over other configuration providers.
        // Configuration updates made via WithConfiguration are written into this provider.
        builder.ConfigureAppConfiguration(configBuilder =>
            configBuilder.Add(new MutableConfigurationSource(_mutableConfigurationProvider)));
        var host = base.CreateHost(builder);
        return host;
    }

    void IHostBuilderConfiguration.AddHostBuilderConfiguration(Action<IHostBuilder> configureHostBuilder)
    {
        _configureHostBuilderActions.Add(configureHostBuilder);
    }
}

internal class MutableConfigurationProvider : ConfigurationProvider
{
    private readonly object _lock = new();

    public void Update(string key, string? value)
    {
        lock (_lock)
        {
            Data[key] = value;
        }
        OnReload();
    }

    public override bool TryGet(string key, out string? value)
    {
        lock (_lock)
        {
            return Data.TryGetValue(key, out value);
        }
    }
}

internal class MutableConfigurationSource : IConfigurationSource
{
    private readonly MutableConfigurationProvider _provider;

    public MutableConfigurationSource(MutableConfigurationProvider provider) => _provider = provider;

    public IConfigurationProvider Build(IConfigurationBuilder builder) => _provider;
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
