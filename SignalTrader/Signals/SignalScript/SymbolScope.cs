namespace SignalTrader.Signals.SignalScript;

public class SymbolScope
{
    #region Enums

    public enum ScopeType
    {
        Global,
        Signal,
        Account,
        Function,
        Local
    }

    #endregion

    #region Members

    private readonly Dictionary<string, Symbol> _symbols = new ();

    #endregion

    #region Properties

    public int Id { get; }
    public ScopeType Type { get; }
    public SymbolScope? EnclosingScope { get; }

    #endregion

    #region Constructors

    public SymbolScope(int id, ScopeType type, SymbolScope? enclosingScope = null)
    {
        Id = id;
        Type = type;
        EnclosingScope = enclosingScope;
    }

    #endregion

    #region Public

    public void Define(string name, ValueWrapper value)
    {
        var symbol = new Symbol(name, value);
        symbol.SymbolScope = this;
        _symbols[name] = symbol;
    }

    public Symbol? Resolve(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
        {
            return symbol;
        }

        if (EnclosingScope != null)
        {
            return EnclosingScope.Resolve(name);
        }
        
        return null;
    }

    public Symbol? GetSymbol(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
        {
            return symbol;
        }

        return null;
    }

    public ISet<string> GetNames()
    {
        return _symbols.Keys.ToHashSet();
    }

    public override string ToString()
    {
        return string.Join(", ", _symbols.Keys);
    }

    #endregion
}
