using System.Net;
using AstroLab.Infrastructure.Archives;
using AstroLab.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Polly.Timeout;

namespace AstroLab.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    private const int ArchiveApiRetryMaxAttempts = 3;
    private const int ArchiveApiRetryDelaySeconds = 2;
    private const int ArchiveApiAttemptTimeoutSeconds = 30;
    private const int ArchiveApiTotalRequestTimeoutSeconds = 60;
    private const int ArchiveApiCircuitBreakerSamplingDurationSeconds = ArchiveApiAttemptTimeoutSeconds * 2;

    extension(IServiceCollection services)
    {
        public void AddAstroLabInfrastructure(IConfiguration configuration)
        {
            services.Configure<LocalFileStoreOptions>(configuration.GetSection(LocalFileStoreOptions.SectionName));

            services.Configure<EsoArchiveOptions>(configuration.GetSection(EsoArchiveOptions.SectionName));

            services.Configure<MastArchiveOptions>(configuration.GetSection(MastArchiveOptions.SectionName));

            services.TryAddSingleton<ILocalFileStore, LocalFileStore>();

            services.TryAddSingleton<FitsDatasetReader>();

            AddEsoArchiveClients(services, configuration);

            AddMastArchiveClients(services, configuration);
        }
    }

    private static void AddEsoArchiveClients(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(EsoArchiveOptions.SectionName).Get<EsoArchiveOptions>() ?? new EsoArchiveOptions();

        var baseAddress = new Uri(options.BaseAddress);

        services
            .AddHttpClient<IEsoArchiveApiClient, EsoArchiveApiClient>(client => client.BaseAddress = baseAddress)
            .AddStandardResilienceHandler(ConfigureArchiveApiResilience);

        services.AddHttpClient<IEsoArchiveDownloadClient, EsoArchiveDownloadClient>(client =>
        {
            client.BaseAddress = baseAddress;
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        services.TryAddTransient<IEsoArchiveClient, EsoArchiveClient>();
    }

    private static void AddMastArchiveClients(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(MastArchiveOptions.SectionName).Get<MastArchiveOptions>() ?? new MastArchiveOptions();

        var baseAddress = new Uri(options.BaseAddress);

        services
            .AddHttpClient<IMastArchiveApiClient, MastArchiveApiClient>(client => client.BaseAddress = baseAddress)
            .AddStandardResilienceHandler(ConfigureArchiveApiResilience);

        services.AddHttpClient<IMastArchiveDownloadClient, MastArchiveDownloadClient>(client =>
        {
            client.BaseAddress = baseAddress;
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        services.TryAddTransient<IMastArchiveClient, MastArchiveClient>();
    }

    private static void ConfigureArchiveApiResilience(HttpStandardResilienceOptions options)
    {
        options.Retry.MaxRetryAttempts = ArchiveApiRetryMaxAttempts;

        options.Retry.Delay = TimeSpan.FromSeconds(ArchiveApiRetryDelaySeconds);

        options.Retry.ShouldHandle = args => ValueTask.FromResult(
            args.Outcome.Exception is HttpRequestException or TimeoutRejectedException
            || args.Outcome.Result?.StatusCode is >= HttpStatusCode.InternalServerError);

        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(ArchiveApiAttemptTimeoutSeconds);

        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(ArchiveApiTotalRequestTimeoutSeconds);

        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(ArchiveApiCircuitBreakerSamplingDurationSeconds);
    }
}
