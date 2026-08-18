using System.Net;
using AstroLab.Infrastructure.Archives;
using AstroLab.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Polly.Timeout;

namespace AstroLab.Infrastructure;

/// <summary>Registers every Infrastructure-layer service: local storage and archive clients. Image rendering (<see cref="AstroLab.Infrastructure.ImageRendering.FitsImageRenderer"/>) is fully static and needs no registration.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    private const int ArchiveRetryMaxAttempts = 3;
    private const int ArchiveRetryDelaySeconds = 2;
    private const int ArchiveAttemptTimeoutMinutes = 30;
    private const int ArchiveTotalRequestTimeoutMinutes = 60;
    private const int ArchiveCircuitBreakerSamplingDurationMinutes = (ArchiveAttemptTimeoutMinutes * 2) + 1;
    private const int ArchiveClientTimeoutMinutes = ArchiveTotalRequestTimeoutMinutes + 5;

    extension(IServiceCollection services)
    {
        public IServiceCollection AddAstroLabInfrastructure(IConfiguration configuration)
        {
            services.Configure<LocalFileStoreOptions>(configuration.GetSection(LocalFileStoreOptions.SectionName));
            services.Configure<EsoArchiveOptions>(configuration.GetSection(EsoArchiveOptions.SectionName));
            services.Configure<MastArchiveOptions>(configuration.GetSection(MastArchiveOptions.SectionName));

            services.TryAddSingleton<ILocalFileStore, LocalFileStore>();
            services.TryAddSingleton<FitsDatasetReader>();

            var esoBaseAddress = new Uri(configuration[$"{EsoArchiveOptions.SectionName}:{nameof(EsoArchiveOptions.BaseAddress)}"]
                ?? new EsoArchiveOptions().BaseAddress);
            services.AddHttpClient<IEsoArchiveClient, EsoArchiveClient>(client =>
                {
                    client.BaseAddress = esoBaseAddress;
                    client.Timeout = TimeSpan.FromMinutes(ArchiveClientTimeoutMinutes);
                })
                .AddStandardResilienceHandler(ConfigureArchiveResilience);

            var mastBaseAddress = new Uri(configuration[$"{MastArchiveOptions.SectionName}:{nameof(MastArchiveOptions.BaseAddress)}"]
                ?? new MastArchiveOptions().BaseAddress);
            services.AddHttpClient<IMastArchiveClient, MastArchiveClient>(client =>
                {
                    client.BaseAddress = mastBaseAddress;
                    client.Timeout = TimeSpan.FromMinutes(ArchiveClientTimeoutMinutes);
                })
                .AddStandardResilienceHandler(ConfigureArchiveResilience);

            return services;
        }
    }

    private static void ConfigureArchiveResilience(HttpStandardResilienceOptions options)
    {
        options.Retry.MaxRetryAttempts = ArchiveRetryMaxAttempts;
        options.Retry.Delay = TimeSpan.FromSeconds(ArchiveRetryDelaySeconds);
        options.Retry.ShouldHandle = args => ValueTask.FromResult(
            args.Outcome.Exception is HttpRequestException or TimeoutRejectedException
            || args.Outcome.Result?.StatusCode is >= HttpStatusCode.InternalServerError);
        options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(ArchiveAttemptTimeoutMinutes);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(ArchiveTotalRequestTimeoutMinutes);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(ArchiveCircuitBreakerSamplingDurationMinutes);
    }
}
