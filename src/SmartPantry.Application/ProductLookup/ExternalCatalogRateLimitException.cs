using System;

namespace SmartPantry.ProductLookup;

public class ExternalCatalogRateLimitException : Exception
{
    public ExternalCatalogRateLimitException()
        : base("El catálogo externo limitó temporalmente las consultas.")
    {
    }
}