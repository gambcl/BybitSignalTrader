namespace SignalTrader.Signals.SignalScript.Exceptions;

public class SignalScriptSecurityException : SignalScriptException
{
    public SignalScriptSecurityException()
    {
    }

    public SignalScriptSecurityException(string message)
        : base(message)
    {
    }

    public SignalScriptSecurityException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
