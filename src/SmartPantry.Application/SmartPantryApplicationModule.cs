using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.Mapperly;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;
using SmartPantry.ExpirationWarnings;

namespace SmartPantry;

[DependsOn(
    typeof(SmartPantryDomainModule),
    typeof(SmartPantryApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class SmartPantryApplicationModule : AbpModule
{
    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        await context.AddBackgroundWorkerAsync<ExpirationWarningWorker>();
    }
}