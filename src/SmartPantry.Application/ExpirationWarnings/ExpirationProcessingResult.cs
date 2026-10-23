namespace SmartPantry.ExpirationWarnings;

public class ExpirationProcessingResult
{
    public int Created { get; set; }
    public int Updated { get; set; }      
    public int Deactivated { get; set; }
    public int Failed { get; set; }
}