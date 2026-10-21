using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.PantryItems;

public class PantryItem : AggregateRoot<Guid>
{
    public Guid OwnerId { get; private set; }
    public Guid ProductId { get; private set; }
    public PantryItemStatus Status { get; private set; }
    public DateOnly? ExpirationDate { get; private set; }

    private PantryItem()
    {
    }

    public PantryItem(Guid id, Guid ownerId, Guid productId, DateOnly? expirationDate)
        : base(id)
    {
        OwnerId = Check.NotDefaultOrNull<Guid>(ownerId, nameof(ownerId));
        ProductId = Check.NotDefaultOrNull<Guid>(productId, nameof(productId));
        Status = PantryItemStatus.Active;
        ExpirationDate = expirationDate;
    }

    public void SetExpirationDate(DateOnly? expirationDate)
    {
        ExpirationDate = expirationDate;
    }

    public void MarkAsConsumed()
    {
        Status = PantryItemStatus.Consumed;
    }
}