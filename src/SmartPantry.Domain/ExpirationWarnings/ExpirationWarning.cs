using System;
using SmartPantry.ExpirationWarnings;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.ExpirationWarnings;

public class ExpirationWarning : AggregateRoot<Guid>
{
    public Guid PantryItemId { get; private set; }
    public ExpirationWarningType Type { get; private set; }
    public DateOnly ExpirationDate { get; private set; }
    public bool IsActive { get; private set; }

    private ExpirationWarning()
    {
    }

    public ExpirationWarning(
        Guid id,
        Guid pantryItemId,
        ExpirationWarningType type,
        DateOnly expirationDate)
        : base(id)
    {
        PantryItemId = Check.NotDefaultOrNull<Guid>(pantryItemId, nameof(pantryItemId));
        Type = type;
        ExpirationDate = expirationDate;
        IsActive = true;
    }

    public void Activate(DateOnly expirationDate)
    {
        ExpirationDate = expirationDate;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}