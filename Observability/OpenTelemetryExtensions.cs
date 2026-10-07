using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SQE_Practice.Observability;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddSqeOpenTelemetry(
        this IServiceCollection services)
    {
        services
            .AddOpenTelemetry()

            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: SqeTelemetry.ServiceName);
            })

            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(
                        SqeTelemetry.ServiceName)

                    .AddAspNetCoreInstrumentation()

                    .AddConsoleExporter();
            })

            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(
                        SqeTelemetry.ServiceName)

                    .AddAspNetCoreInstrumentation()

                    .AddConsoleExporter();
            });

        return services;
    }
}