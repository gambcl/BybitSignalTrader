namespace SignalTrader.Signals.SignalScript.Exceptions;

public class SignalScriptValidationException : SignalScriptException
{
    public SignalScriptValidationException()
    {
    }

    public SignalScriptValidationException(string message)
        : base(message)
    {
    }

    public SignalScriptValidationException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
