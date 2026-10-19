using System;

namespace SmartPantry.ProductLookup;

public class ExternalCatalogUnavailableException : Exception
{
    public ExternalCatalogUnavailableException(string message)
        : base(message)
    {
    }

    public ExternalCatalogUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}