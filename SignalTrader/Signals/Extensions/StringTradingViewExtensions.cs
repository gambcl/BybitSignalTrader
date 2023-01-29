using System.Text.RegularExpressions;

namespace SignalTrader.Signals.Extensions;

public static class StringTradingViewExtensions
{
    /// <summary>
    /// Converts a string in the TradingView interval format to the TradingView timeframe format.
    /// e.g. "30" becomes "30m", "60" becomes "1h"
    /// Intervals from 1D and above are returned unchanged.
    /// </summary>
    /// <param name="value">A string in the TradingView interval format</param>
    /// <returns>A string in the TradingView timeframe format</returns>
    public static string ToTradingViewTimeframe(this string value)
    {
        const long minutesPerHour = 60;
        const long minutesPerDay = minutesPerHour * 24;

        if (Regex.IsMatch(value, @"^[0-9]+$"))
        {
            // Timeframes in Hours, Minutes
            long numeric = long.Parse(value);
            long numHours = numeric / minutesPerHour;
            if (numHours > 0)
            {
                return $"{numHours}h";
            }
        
            return $"{numeric}m";
        }
        
        // Timeframes from Daily and upwards.
        return value;
    }
}
