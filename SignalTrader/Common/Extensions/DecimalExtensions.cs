using System.Text.RegularExpressions;

namespace SignalTrader.Common.Extensions;

public static class DecimalExtensions
{
    public static decimal TruncateToStepSize(this decimal value, string step)
    {
        if (Regex.IsMatch(step, @"^0\.0*1$"))
        {
            var ticks = step.IndexOf('1') - 1;
            decimal factor = (decimal) Math.Pow(10, ticks);
            decimal truncated = Math.Floor(value * factor) / factor;
            return truncated;
        }

        return value;
    }

    public static decimal TruncateToDecimalPlaces(this decimal value, uint decimalPlaces)
    {
        decimal factor = (decimal) Math.Pow(10, decimalPlaces);
        decimal truncated = Math.Floor(value * factor) / factor;
        return truncated;
    }
}
