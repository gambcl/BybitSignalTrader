namespace SignalTrader.Signals.SignalScript.Exceptions;

public class SignalScriptSyntaxException : SignalScriptException
{
    public SignalScriptSyntaxException()
    {
    }

    public SignalScriptSyntaxException(string message)
        : base(message)
    {
    }

    public SignalScriptSyntaxException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
