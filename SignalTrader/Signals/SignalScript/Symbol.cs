namespace SignalTrader.Signals.SignalScript;

/// <summary>
/// Represents a symbol in SignalScript.
/// </summary>
public class Symbol
{
    #region Constructors

    public Symbol(string name, ValueWrapper value)
    {
        Name = name;
        Value = value;
    }

    #endregion

    #region Properties

    public string Name { get; }
    public ValueWrapper Value { get; }
    public SymbolScope SymbolScope { get; set; } = null!;

    #endregion

    #region Public

    public override string ToString()
    {
        return $"<{Name}:{Value.Type}:{Value.Text}>";
    }

    #endregion
}
