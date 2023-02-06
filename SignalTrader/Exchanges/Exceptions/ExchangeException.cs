namespace SignalTrader.Exchanges.Exceptions;

public class ExchangeException : Exception
{
    public ExchangeException()
    {
    }

    public ExchangeException(string message)
        : base(message)
    {
    }

    public ExchangeException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
