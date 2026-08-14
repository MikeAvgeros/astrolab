using AstroLab.Infrastructure.ESO;
using AstroLab.Infrastructure.ImageRendering;
using AstroLab.Infrastructure.MAST;
using AstroLab.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AstroLab.Infrastructure;

/// <summary>Registers every Infrastructure-layer service: local storage, archive clients, and image rendering.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAstroLabInfrastructure(IConfiguration configuration)
        {
            services.Configure<LocalFileStoreOptions>(configuration.GetSection(LocalFileStoreOptions.SectionName));
            services.Configure<EsoArchiveOptions>(configuration.GetSection(EsoArchiveOptions.SectionName));
            services.Configure<MastArchiveOptions>(configuration.GetSection(MastArchiveOptions.SectionName));

            services.TryAddSingleton<ILocalFileStore, LocalFileStore>();
            services.TryAddSingleton<FitsImageRenderer>();
            services.TryAddSingleton<FitsDatasetReader>();

            var esoBaseAddress = new Uri(configuration[$"{EsoArchiveOptions.SectionName}:{nameof(EsoArchiveOptions.BaseAddress)}"]
                ?? new EsoArchiveOptions().BaseAddress);
            services.AddHttpClient<IEsoArchiveClient, EsoArchiveClient>(client => client.BaseAddress = esoBaseAddress)
                .AddStandardResilienceHandler();

            var mastBaseAddress = new Uri(configuration[$"{MastArchiveOptions.SectionName}:{nameof(MastArchiveOptions.BaseAddress)}"]
                ?? new MastArchiveOptions().BaseAddress);
            services.AddHttpClient<IMastArchiveClient, MastArchiveClient>(client => client.BaseAddress = mastBaseAddress)
                .AddStandardResilienceHandler();

            return services;
        }
    }
}
