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
    public decimal? Quantity { get; private set; }
    public string? Unit { get; private set; }

    private PantryItem()
    {
    }

    public PantryItem(
        Guid id,
        Guid ownerId,
        Guid productId,
        DateOnly? expirationDate,
        decimal? quantity = null,
        string? unit = null)
        : base(id)
    {
        OwnerId = Check.NotDefaultOrNull<Guid>(ownerId, nameof(ownerId));
        ProductId = Check.NotDefaultOrNull<Guid>(productId, nameof(productId));
        Status = PantryItemStatus.Active;
        ExpirationDate = expirationDate;
        SetQuantityAndUnit(quantity, unit);
    }

    public void SetExpirationDate(DateOnly? expirationDate)
    {
        ExpirationDate = expirationDate;
    }

    public void UpdateDetails(decimal? quantity, string? unit, DateOnly? expirationDate)
    {
        SetQuantityAndUnit(quantity, unit);
        ExpirationDate = expirationDate;
    }

    public void MarkAsConsumed()
    {
        Status = PantryItemStatus.Consumed;
    }

    private void SetQuantityAndUnit(decimal? quantity, string? unit)
    {
        if (quantity.HasValue && quantity.Value <= 0)
        {
            throw new BusinessException(SmartPantryDomainErrorCodes.QuantityMustBeGreaterThanZero)
                .WithData("Quantity", quantity.Value);
        }

        Quantity = quantity;
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
    }
}