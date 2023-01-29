namespace SignalTrader.Signals.SignalScript.Exceptions;

public class ValueWrapperException : Exception
{
    public ValueWrapperException()
    {
    }

    public ValueWrapperException(string message)
        : base(message)
    {
    }

    public ValueWrapperException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
