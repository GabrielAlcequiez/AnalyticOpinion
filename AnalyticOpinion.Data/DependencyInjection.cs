using AnalyticOpinion.Data.Interfaces;
using AnalyticOpinion.Data.Configuration;
using AnalyticOpinion.Data.Extractors;
using AnalyticOpinion.Data.Persistence;
using AnalyticOpinion.Data.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnalyticOpinion.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddExtraction(this IServiceCollection services, IConfiguration configuration)
    {
        // La configuración de cada fuente se valida al arrancar: si falta o es inválida, el Worker no inicia.
        services.AddValidatedOptions<CsvSourceOptions>(configuration, CsvSourceOptions.SectionName);
        services.AddValidatedOptions<DatabaseSourceOptions>(configuration, DatabaseSourceOptions.SectionName);
        services.AddValidatedOptions<ApiSourceOptions>(configuration, ApiSourceOptions.SectionName);
        services.AddValidatedOptions<StagingOptions>(configuration, StagingOptions.SectionName);

        services.TryAddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<IStagingRepository, StagingRepository>();

        // Para agregar una fuente nueva basta con registrar otro IExtractor aquí.
        services.AddTransient<IExtractor, CsvExtractor>();
        services.AddTransient<IExtractor, DatabaseExtractor>();

        services.AddHttpClient<ApiExtractor>()
            .AddStandardResilienceHandler(); // reintentos con backoff, timeouts y circuit breaker
        services.AddTransient<IExtractor>(sp => sp.GetRequiredService<ApiExtractor>());

        services.AddTransient<ExtractionService>();

        return services;
    }

    public static IServiceCollection AddSourceRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<SocialCommentsOptions>(configuration, SocialCommentsOptions.SectionName);

        services.TryAddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<ISocialCommentRepository, SocialCommentRepository>();

        return services;
    }

    private static void AddValidatedOptions<TOptions>(
        this IServiceCollection services, IConfiguration configuration, string sectionName)
        where TOptions : class =>
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
