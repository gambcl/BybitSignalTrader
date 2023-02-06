using SignalTrader.Common.Enums;

namespace SignalTrader.Common.Extensions;

public static class DirectionExtensions
{
    public static string ToEmoji(this Direction value)
    {
        return value switch
        {
            Direction.Long => Telegram.Constants.Emojis.CircleGreen,
            Direction.Short => Telegram.Constants.Emojis.CircleRed,
            _ => string.Empty
        };
    }
}
