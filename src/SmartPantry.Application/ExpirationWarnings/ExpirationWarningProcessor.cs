using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SmartPantry.PantryItems;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Uow;

namespace SmartPantry.ExpirationWarnings;

public class ExpirationWarningProcessor : ITransientDependency
{
    private const ExpirationWarningType WarningType = ExpirationWarningType.Expiration;

    private readonly IRepository<PantryItem, Guid> _items;
    private readonly IRepository<ExpirationWarning, Guid> _warnings;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IUnitOfWorkManager _uowManager;

    public ILogger<ExpirationWarningProcessor> Logger { get; set; } =
        NullLogger<ExpirationWarningProcessor>.Instance;

    public ExpirationWarningProcessor(
        IRepository<PantryItem, Guid> items,
        IRepository<ExpirationWarning, Guid> warnings,
        IGuidGenerator guidGenerator,
        IUnitOfWorkManager uowManager)
    {
        _items = items;
        _warnings = warnings;
        _guidGenerator = guidGenerator;
        _uowManager = uowManager;
    }

    /// <summary>
    /// Crea, actualiza o desactiva la advertencia de vencimiento de cada ítem.
    /// "today" se recibe por parámetro; quien llama decide el criterio de fecha (UTC en el worker).
    /// </summary>
    public async Task<ExpirationProcessingResult> ProcessAsync(DateOnly today)
    {
        var result = new ExpirationProcessingResult();
        var limit = today.AddDays(ExpirationWarningConsts.WarningThresholdDays);

        var itemIds = new HashSet<Guid>();
        using (var uow = _uowManager.Begin(requiresNew: true, isTransactional: false))
        {
            var due = await _items.GetListAsync(x =>
                x.Status == PantryItemStatus.Active &&
                x.ExpirationDate != null &&
                x.ExpirationDate <= limit);
            foreach (var item in due)
            {
                itemIds.Add(item.Id);
            }

            var active = await _warnings.GetListAsync(x => x.Type == WarningType && x.IsActive);
            foreach (var warning in active)
            {
                itemIds.Add(warning.PantryItemId);
            }

            await uow.CompleteAsync();
        }

        foreach (var itemId in itemIds)
        {
            try
            {
                await ReconcileAsync(itemId, today, limit, result);
            }
            catch (Exception ex)
            {
                
                result.Failed++;
                Logger.LogWarning(ex, "No se pudo procesar el vencimiento del ítem {ItemId}.", itemId);
            }
        }

        return result;
    }

    private async Task ReconcileAsync(
        Guid itemId, DateOnly today, DateOnly limit, ExpirationProcessingResult result)
    {
        using var uow = _uowManager.Begin(requiresNew: true, isTransactional: true);

        var item = await _items.FindAsync(itemId);
        if (item == null)
        {
            return; 
        }

        var warning = await _warnings.FindAsync(x =>
            x.PantryItemId == itemId && x.Type == WarningType);

        var shouldWarn =
            item.Status == PantryItemStatus.Active &&
            item.ExpirationDate is { } date &&
            date <= limit;

        if (shouldWarn)
        {
            var expiration = item.ExpirationDate!.Value;

            if (warning == null)
            {
                await _warnings.InsertAsync(new ExpirationWarning(
                    _guidGenerator.Create(), item.Id, WarningType, expiration));
                result.Created++;
            }
            else if (!warning.IsActive || warning.ExpirationDate != expiration)
            {
                warning.Activate(expiration);
                await _warnings.UpdateAsync(warning);
                result.Updated++;
            }
        }
        else if (warning is { IsActive: true })
        {
            warning.Deactivate();
            await _warnings.UpdateAsync(warning);
            result.Deactivated++;
        }

        await uow.CompleteAsync();
    }
}