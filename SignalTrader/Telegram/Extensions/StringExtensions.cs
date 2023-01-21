namespace SignalTrader.Telegram.Extensions;

public static class StringExtensions
{
    public static string ToTelegramSafeString(this string value)
    {
        char[] mustBeEscaped = { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
        if (!string.IsNullOrWhiteSpace(value))
        {
            var result = value;
            foreach (var c in mustBeEscaped)
            {
                result = result.Replace(c.ToString(), "\\" + c);
            }

            return result;
        }

        return value;
    }
}
