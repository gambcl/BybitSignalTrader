namespace SignalTrader.Signals.SignalScript;

public class SymbolTable
{
    #region Members

    private readonly Stack<SymbolScope> _scopeStack = new ();
    private readonly List<SymbolScope> _allScopes = new ();
    private int _id = 0;

    #endregion

    #region Constructors

    public SymbolTable()
    {
        // Add a global scope.
        var globals = new SymbolScope(NextId(), SymbolScope.ScopeType.Global, null);
        _scopeStack.Push(globals);
        _allScopes.Add(globals);
    }

    #endregion

    #region Public

    public SymbolScope PushScope(SymbolScope.ScopeType scopeType)
    {
        var enclosingScope = _scopeStack.Peek();
        var scope = new SymbolScope(NextId(), scopeType, enclosingScope);
        _scopeStack.Push(scope);
        _allScopes.Add(scope);
        return scope;
    }

    public void PopScope()
    {
        _scopeStack.Pop();
    }

    public SymbolScope CurrentScope()
    {
        if (_scopeStack.Count > 0)
        {
            return _scopeStack.Peek();
        }

        throw new ApplicationException("Unbalanced Scope stack");
    }

    public SymbolScope? GetScope(int id)
    {
        return _allScopes.FirstOrDefault(scope => scope.Id == id);
    }
    
    #endregion

    #region Private

    private int NextId()
    {
        _id++;
        return _id;
    }

    #endregion
}
