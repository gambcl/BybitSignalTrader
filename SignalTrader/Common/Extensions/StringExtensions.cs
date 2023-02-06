namespace SignalTrader.Common.Extensions;

public static class StringExtensions
{
    public static string Pluralize(this string value, int count, string? plural = null)
    {
        if (count == 1)
        {
            // Singular.
            return value;
        }
        
        // Plural.
        return plural != null ? plural : $"{value}s";
    }
}
