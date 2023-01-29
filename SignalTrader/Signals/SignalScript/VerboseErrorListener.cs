using Antlr4.Runtime;
using SignalTrader.Signals.SignalScript.Exceptions;

namespace SignalTrader.Signals.SignalScript;

/// <summary>
/// The default behaviour of ANTLR is to try to recover from parsing errors, we want to fail fast instead.
/// </summary>
public class VerboseErrorListener : BaseErrorListener
{
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        throw new SignalScriptSyntaxException($"Syntax error: {msg} [line {line}, position {charPositionInLine}]");
    }
}
