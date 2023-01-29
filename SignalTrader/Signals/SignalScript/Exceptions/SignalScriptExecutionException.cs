namespace SignalTrader.Signals.SignalScript.Exceptions;

public class SignalScriptExecutionException : SignalScriptException
{
    public SignalScriptExecutionException()
    {
    }

    public SignalScriptExecutionException(string message)
        : base(message)
    {
    }

    public SignalScriptExecutionException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
