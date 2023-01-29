namespace SignalTrader.Signals.SignalScript.Exceptions;

public class SignalScriptException : Exception
{
    public SignalScriptException()
    {
    }

    public SignalScriptException(string message)
        : base(message)
    {
    }

    public SignalScriptException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
