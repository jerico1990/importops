using ImportOps.Application.Alerts;

namespace ImportOps.Api.Services;

public sealed class AlertEvaluationHostedService(IServiceScopeFactory scopes,ILogger<AlertEvaluationHostedService> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer=new PeriodicTimer(TimeSpan.FromMinutes(5));
        while(!stoppingToken.IsCancellationRequested)
        {
            try{using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<IAlertService>().EvaluateAllCompaniesAsync(stoppingToken);}
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogError(ex,"Error evaluando alertas de ImportOps.");}
            try{if(!await timer.WaitForNextTickAsync(stoppingToken))break;}catch(OperationCanceledException){break;}
        }
    }
}
