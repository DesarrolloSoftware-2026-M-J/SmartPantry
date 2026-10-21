using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace SmartPantry.ExpirationWarnings;

/// <summary>
/// Solo decide CUÁNDO ejecutar. La regla (QUÉ advertencias crear o actualizar)
/// vive en ExpirationWarningProcessor, que se puede probar sin esperar el temporizador.
/// </summary>
public class ExpirationWarningWorker : AsyncPeriodicBackgroundWorkerBase
{
    private const int PeriodInMilliseconds = 60 * 60 * 1000; 

    public ExpirationWarningWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = PeriodInMilliseconds;
        Timer.RunOnStart = true; 
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        try
        {
            var processor = workerContext.ServiceProvider.GetRequiredService<ExpirationWarningProcessor>();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var result = await processor.ProcessAsync(today);

            Logger.LogInformation(
                "Vencimientos procesados ({Today}): {Created} creadas, {Updated} actualizadas, {Deactivated} desactivadas, {Failed} con error.",
                today, result.Created, result.Updated, result.Deactivated, result.Failed);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Falló la ejecución del worker de vencimientos.");
        }
    }
}