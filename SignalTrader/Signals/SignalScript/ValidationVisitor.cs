using SignalTrader.Signals.Services;
using SignalTrader.Signals.SignalScript.Exceptions;
using SignalTrader.Signals.SignalScript.Generated;

namespace SignalTrader.Signals.SignalScript;

/// <summary>
/// Visitor to validate the SignalScript parse tree:
/// - Check functions are being passed all required parameters
/// - Check parameters are of correct type
/// </summary>
public class ValidationVisitor : SignalScriptBaseVisitor<Task<ValueWrapper?>>
{
    protected record AllowedSymbol(string Name, ValueWrapper.ValueType[] AllowedTypes);
    
    #region Members

    protected readonly SymbolTable _symbolTable = new();
    protected readonly IServiceScopeFactory _serviceScopeFactory;

    #endregion

    #region Constructors

    public ValidationVisitor(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    #endregion

    #region Visitor

    protected override Task<ValueWrapper?> DefaultResult => Task.FromResult(default(ValueWrapper?));

    public override async Task<ValueWrapper?> VisitSignal(SignalScriptParser.SignalContext context)
    {
        using var serviceScope = _serviceScopeFactory.CreateScope();
        var signalScriptService = serviceScope.ServiceProvider.GetRequiredService<ISignalScriptService>();
        
        // Push a new SymbolScope for this function.
        _symbolTable.PushScope(SymbolScope.ScopeType.Signal);
        
        // Add default parameter values to function's scope.
        AddDefaultSignalFunctionParameters(_symbolTable.CurrentScope());
        
        // Visit all function parameters, which will be added to current SymbolScope.
        if (context.parameters != null)
        {
            await Visit(context.parameters);
        }
        
        // Validate signal() function parameters.
        ValidateSignalFunctionParameters(_symbolTable.CurrentScope());
        
        // Before accepting the signal, check the passphrase.
        var signalPassphrase = _symbolTable.CurrentScope().Resolve(Constants.SignalFunction.ParameterNames.Passphrase)?.Value.Text;
        if (!signalScriptService.ValidatePassphrase(signalPassphrase))
        {
            throw new SignalScriptSecurityException("Invalid passphrase");
        }
        
        // Allow derived classes to execute function (does nothing in ValidationVisitor).
        await ExecuteSignalFunctionAsync(_symbolTable.CurrentScope());
        
        // Visit block, if present.
        if (context.signalblock() != null)
        {
            await Visit(context.signalblock());
        }
        
        // Leaving this SymbolScope.
        _symbolTable.PopScope();
        
        return null;
    }

    public override async Task<ValueWrapper?> VisitAccount(SignalScriptParser.AccountContext context)
    {
        using var serviceScope = _serviceScopeFactory.CreateScope();
        var signalScriptService = serviceScope.ServiceProvider.GetRequiredService<ISignalScriptService>();
        
        var functionName = context.name.Text;
        
        // Validate account name.
        var accountId = await signalScriptService.ValidateAccountNameAsync(functionName);
        if (!accountId.HasValue)
        {
            throw new SignalScriptValidationException($"'{functionName}' is not a valid account name");
        }
        
        // Push a new SymbolScope for this function.
        _symbolTable.PushScope(SymbolScope.ScopeType.Account);
        
        // Add default parameter values to function's scope.
        AddDefaultAccountFunctionParameters(accountId.Value, _symbolTable.CurrentScope());
        
        // Visit all function parameters, which will be added to current SymbolScope.
        if (context.parameters != null)
        {
            await Visit(context.parameters);
        }
        
        // Validate account_name() function parameters.
        ValidateAccountFunctionParameters(functionName, _symbolTable.CurrentScope());
        
        // Allow derived classes to execute function (does nothing in ValidationVisitor).
        await ExecuteAccountAsync(functionName, _symbolTable.CurrentScope());
        
        // Visit block, if present.
        if (context.funclist() != null)
        {
            await Visit(context.funclist());
        }
        
        // Leaving this SymbolScope.
        _symbolTable.PopScope();
        
        return null;
    }

    public override async Task<ValueWrapper?> VisitFunc(SignalScriptParser.FuncContext context)
    {
        var functionName = context.name.Text;
        
        // Push a new SymbolScope for this function.
        _symbolTable.PushScope(SymbolScope.ScopeType.Function);
        
        // Add default parameter values to function's scope.
        AddDefaultFunctionParameters(functionName, _symbolTable.CurrentScope());
        
        // Visit all function parameters, which will be added to current SymbolScope.
        if (context.parameters != null)
        {
            await Visit(context.parameters);
        }
        
        // Validate function parameters.
        ValidateFunctionParameters(functionName, _symbolTable.CurrentScope());
        
        // Allow derived classes to execute function (does nothing in ValidationVisitor).
        await ExecuteAccountFunctionAsync(functionName, _symbolTable.CurrentScope());
        
        // Leaving this SymbolScope.
        _symbolTable.PopScope();
        
        return null;
    }

    public override async Task<ValueWrapper?> VisitNamedparam(SignalScriptParser.NamedparamContext context)
    {
        var name = context.name.Text;
        var value = await Visit(context.value);

        if (!string.IsNullOrWhiteSpace(name) && value != null)
        {
            var scope = _symbolTable.CurrentScope();
            scope.Define(name, value);
        }
        else
        {
            throw new SignalScriptValidationException($"Invalid named parameter '{name}'");
        }
        
        return null;
    }

    public override Task<ValueWrapper?> VisitIdentifierParamValue(SignalScriptParser.IdentifierParamValueContext context)
    {
        var symbolName = context.ID().GetText();
        var resolvedSymbol = _symbolTable.CurrentScope().Resolve(symbolName);
        if (resolvedSymbol == null)
        {
            throw new SignalScriptValidationException($"The name '{symbolName}' does not exist");
        }

        return Task.FromResult(resolvedSymbol.Value)!;
    }

    public override Task<ValueWrapper?> VisitBooleanParamValue(SignalScriptParser.BooleanParamValueContext context)
    {
        return Task.FromResult(new ValueWrapper(ValueWrapper.ValueType.Boolean, context.boolean().GetText()))!;
    }

    public override Task<ValueWrapper?> VisitIntParamValue(SignalScriptParser.IntParamValueContext context)
    {
        return Task.FromResult(new ValueWrapper(ValueWrapper.ValueType.Int, context.INT().GetText()))!;
    }

    public override Task<ValueWrapper?> VisitIntPercentParamValue(SignalScriptParser.IntPercentParamValueContext context)
    {
        return Task.FromResult(new ValueWrapper(ValueWrapper.ValueType.IntPercent, context.INTP().GetText()))!;
    }

    public override Task<ValueWrapper?> VisitFloatParamValue(SignalScriptParser.FloatParamValueContext context)
    {
        return Task.FromResult(new ValueWrapper(ValueWrapper.ValueType.Float, context.FLOAT().GetText()))!;
    }

    public override Task<ValueWrapper?> VisitFloatPercentParamValue(SignalScriptParser.FloatPercentParamValueContext context)
    {
        return Task.FromResult(new ValueWrapper(ValueWrapper.ValueType.FloatPercent, context.FLOATP().GetText()))!;
    }

    public override Task<ValueWrapper?> VisitStringParamValue(SignalScriptParser.StringParamValueContext context)
    {
        return Task.FromResult(new ValueWrapper(ValueWrapper.ValueType.String, context.STRING().GetText()[1..^1]))!;
    }

    public override Task<ValueWrapper?> VisitSideParamValue(SignalScriptParser.SideParamValueContext context)
    {
        return Task.FromResult(new ValueWrapper(ValueWrapper.ValueType.Side, context.side().GetText()))!;
    }

    public override Task<ValueWrapper?> VisitDirectionParamValue(SignalScriptParser.DirectionParamValueContext context)
    {
        return Task.FromResult(new ValueWrapper(ValueWrapper.ValueType.Direction, context.direction().GetText()))!;
    }

    public override Task<ValueWrapper?> VisitPriceParamValue(SignalScriptParser.PriceParamValueContext context)
    {
        return Task.FromResult(new ValueWrapper(ValueWrapper.ValueType.Price, context.price().GetText()))!;
    }

    public override Task<ValueWrapper?> VisitOrderParamValue(SignalScriptParser.OrderParamValueContext context)
    {
        return Task.FromResult(new ValueWrapper(ValueWrapper.ValueType.Order, context.order().GetText()))!;
    }

    public override Task<ValueWrapper?> VisitLeverageParamValue(SignalScriptParser.LeverageParamValueContext context)
    {
        return Task.FromResult(new ValueWrapper(ValueWrapper.ValueType.Leverage, context.leverage().GetText()))!;
    }

    #endregion

    #region Protected

    protected virtual Task ExecuteSignalFunctionAsync(SymbolScope symbolScope)
    {
        // Does nothing in ValidationVisitor.
        return Task.CompletedTask;
    }

    protected virtual Task ExecuteAccountAsync(string accountName, SymbolScope symbolScope)
    {
        // Does nothing in ValidationVisitor.
        return Task.CompletedTask;
    }

    protected virtual Task ExecuteAccountFunctionAsync(string functionName, SymbolScope symbolScope)
    {
        // Does nothing in ValidationVisitor.
        return Task.CompletedTask;
    }

    #endregion

    #region Private

    private void AddDefaultSignalFunctionParameters(SymbolScope symbolScope)
    {
        symbolScope.Define(Constants.SignalFunction.ParameterNames.LongEnabled, new ValueWrapper(ValueWrapper.ValueType.Boolean, "true"));
        symbolScope.Define(Constants.SignalFunction.ParameterNames.ShortEnabled, new ValueWrapper(ValueWrapper.ValueType.Boolean, "true"));
    }
    
    private void ValidateSignalFunctionParameters(SymbolScope symbolScope)
    {
        const string functionName = Constants.FunctionNames.Signal;
        
        AllowedSymbol[] requiredSymbols = {
            new(Constants.SignalFunction.ParameterNames.StrategyName, new [] { ValueWrapper.ValueType.String }),
            new(Constants.SignalFunction.ParameterNames.SignalName, new [] { ValueWrapper.ValueType.String }),
            new(Constants.SignalFunction.ParameterNames.Exchange, new [] { ValueWrapper.ValueType.String }),
            new(Constants.SignalFunction.ParameterNames.Ticker, new [] { ValueWrapper.ValueType.String }),
            new(Constants.SignalFunction.ParameterNames.BaseAsset, new [] { ValueWrapper.ValueType.String }),
            new(Constants.SignalFunction.ParameterNames.QuoteAsset, new [] { ValueWrapper.ValueType.String }),
            new(Constants.SignalFunction.ParameterNames.Interval, new [] { ValueWrapper.ValueType.String }),
            new(Constants.SignalFunction.ParameterNames.SignalTime, new [] { ValueWrapper.ValueType.String }),
            new(Constants.SignalFunction.ParameterNames.BarTime, new [] { ValueWrapper.ValueType.String }),
            new(Constants.SignalFunction.ParameterNames.Open, new [] { ValueWrapper.ValueType.Float }),
            new(Constants.SignalFunction.ParameterNames.High, new [] { ValueWrapper.ValueType.Float }),
            new(Constants.SignalFunction.ParameterNames.Low, new [] { ValueWrapper.ValueType.Float }),
            new(Constants.SignalFunction.ParameterNames.Close, new [] { ValueWrapper.ValueType.Float }),
            new(Constants.SignalFunction.ParameterNames.Volume, new [] { ValueWrapper.ValueType.Float }),
            new(Constants.SignalFunction.ParameterNames.Passphrase, new [] { ValueWrapper.ValueType.String })
        };

        foreach (var requiredSymbol in requiredSymbols)
        {
            ValidateRequiredParameter(requiredSymbol.Name, requiredSymbol.AllowedTypes, symbolScope, functionName);
        }
        
        AllowedSymbol[] optionalSymbols =
        {
            new(Constants.SignalFunction.ParameterNames.LongEnabled, new [] { ValueWrapper.ValueType.Boolean, ValueWrapper.ValueType.Int }),
            new(Constants.SignalFunction.ParameterNames.ShortEnabled, new [] { ValueWrapper.ValueType.Boolean, ValueWrapper.ValueType.Int })
        };

        foreach (var optionalSymbol in optionalSymbols)
        {
            ValidateOptionalParameter(optionalSymbol.Name, optionalSymbol.AllowedTypes, symbolScope, functionName);
        }

        ValidateUnknownParameters(symbolScope, requiredSymbols, optionalSymbols, functionName);
    }

    private void AddDefaultAccountFunctionParameters(long accountId, SymbolScope symbolScope)
    {
        // Define accountId so it is available for all child functions in block.
        symbolScope.Define(Constants.AccountFunction.ParameterNames.AccountId, new ValueWrapper(ValueWrapper.ValueType.Int, accountId.ToString()));
    }
    
    private void ValidateAccountFunctionParameters(string functionName, SymbolScope symbolScope)
    {
        AllowedSymbol[] requiredSymbols =
        {
            new(Constants.AccountFunction.ParameterNames.AccountId, new [] { ValueWrapper.ValueType.String })
        };

        foreach (var requiredSymbol in requiredSymbols)
        {
            ValidateRequiredParameter(requiredSymbol.Name, requiredSymbol.AllowedTypes, symbolScope, functionName);
        }
        
        AllowedSymbol[] optionalSymbols = {
            new(Constants.AccountFunction.ParameterNames.BaseAsset, new [] { ValueWrapper.ValueType.String }),
            new(Constants.AccountFunction.ParameterNames.QuoteAsset, new [] { ValueWrapper.ValueType.String })
        };

        foreach (var optionalSymbol in optionalSymbols)
        {
            ValidateOptionalParameter(optionalSymbol.Name, optionalSymbol.AllowedTypes, symbolScope, functionName);
        }

        ValidateUnknownParameters(symbolScope, requiredSymbols, optionalSymbols, functionName);
    }

    private void AddDefaultFunctionParameters(string functionName, SymbolScope symbolScope)
    {
        switch (functionName)
        {
            case Constants.FunctionNames.CancelOrders:
                break;

            case Constants.FunctionNames.ClosePosition:
                symbolScope.Define(Constants.ClosePositionFunction.ParameterNames.Order, new ValueWrapper(ValueWrapper.ValueType.Order, "market"));
                symbolScope.Define(Constants.ClosePositionFunction.ParameterNames.Offset, new ValueWrapper(ValueWrapper.ValueType.Float, "0.0"));
                break;
            
            case Constants.FunctionNames.OpenPosition:
                symbolScope.Define(Constants.OpenPositionFunction.ParameterNames.Leverage, new ValueWrapper(ValueWrapper.ValueType.Float, "1.0"));
                symbolScope.Define(Constants.OpenPositionFunction.ParameterNames.LeverageType, new ValueWrapper(ValueWrapper.ValueType.Leverage, "isolated"));
                symbolScope.Define(Constants.OpenPositionFunction.ParameterNames.Order, new ValueWrapper(ValueWrapper.ValueType.Order, "market"));
                symbolScope.Define(Constants.OpenPositionFunction.ParameterNames.Offset, new ValueWrapper(ValueWrapper.ValueType.Float, "0.0"));
                break;
        }
    }
    
    private void ValidateFunctionParameters(string functionName, SymbolScope symbolScope)
    {
        AllowedSymbol[] requiredSymbols = { };
        AllowedSymbol[] optionalSymbols = { };

        switch (functionName)
        {
            case Constants.FunctionNames.CancelOrders:
            {
                requiredSymbols = new AllowedSymbol[]{};
                optionalSymbols = new AllowedSymbol[]{
                    new(Constants.CancelOrdersFunction.ParameterNames.AccountId, new [] { ValueWrapper.ValueType.String }),
                    new(Constants.CancelOrdersFunction.ParameterNames.BaseAsset, new [] { ValueWrapper.ValueType.String }),
                    new(Constants.CancelOrdersFunction.ParameterNames.QuoteAsset, new [] { ValueWrapper.ValueType.String }),
                    new(Constants.CancelOrdersFunction.ParameterNames.Side, new [] { ValueWrapper.ValueType.Side })
                };
                break;
            }
            
            case Constants.FunctionNames.ClosePosition:
            {
                requiredSymbols = new AllowedSymbol[]{
                    new(Constants.ClosePositionFunction.ParameterNames.Direction, new [] { ValueWrapper.ValueType.Direction }),
                    new(Constants.ClosePositionFunction.ParameterNames.Order, new [] { ValueWrapper.ValueType.Order })
                };
                optionalSymbols = new AllowedSymbol[]{
                    new(Constants.ClosePositionFunction.ParameterNames.AccountId, new [] { ValueWrapper.ValueType.String }),
                    new(Constants.ClosePositionFunction.ParameterNames.BaseAsset, new [] { ValueWrapper.ValueType.String }),
                    new(Constants.ClosePositionFunction.ParameterNames.QuoteAsset, new [] { ValueWrapper.ValueType.String }),
                    new(Constants.ClosePositionFunction.ParameterNames.Price, new [] { ValueWrapper.ValueType.Float, ValueWrapper.ValueType.Price }),
                    new(Constants.ClosePositionFunction.ParameterNames.Offset, new [] { ValueWrapper.ValueType.Float, ValueWrapper.ValueType.FloatPercent, ValueWrapper.ValueType.IntPercent })
                };
                
                // Require "price","offset" if "order"==limit
                var orderSymbol = symbolScope.Resolve(Constants.ClosePositionFunction.ParameterNames.Order);
                if (orderSymbol != null && orderSymbol.Value.Text.Equals("limit"))
                {
                    var priceSymbol = symbolScope.Resolve(Constants.ClosePositionFunction.ParameterNames.Price);
                    if (priceSymbol == null)
                    {
                        throw new SignalScriptValidationException($"{functionName}() parameter '{Constants.ClosePositionFunction.ParameterNames.Price}' is required when using limit orders");
                    }
                    var offsetSymbol = symbolScope.Resolve(Constants.ClosePositionFunction.ParameterNames.Offset);
                    if (offsetSymbol == null)
                    {
                        throw new SignalScriptValidationException($"{functionName}() parameter '{Constants.ClosePositionFunction.ParameterNames.Offset}' is required when using limit orders");
                    }
                }
                break;
            }
            
            case Constants.FunctionNames.OpenPosition:
            {
                requiredSymbols = new AllowedSymbol[]{
                    new(Constants.OpenPositionFunction.ParameterNames.Direction, new [] { ValueWrapper.ValueType.Direction }),
                    new(Constants.OpenPositionFunction.ParameterNames.Order, new [] { ValueWrapper.ValueType.Order })
                };
                optionalSymbols = new AllowedSymbol[]{
                    new(Constants.OpenPositionFunction.ParameterNames.AccountId, new [] { ValueWrapper.ValueType.String }),
                    new(Constants.OpenPositionFunction.ParameterNames.BaseAsset, new [] { ValueWrapper.ValueType.String }),
                    new(Constants.OpenPositionFunction.ParameterNames.QuoteAsset, new [] { ValueWrapper.ValueType.String }),
                    new(Constants.OpenPositionFunction.ParameterNames.Leverage, new [] { ValueWrapper.ValueType.Float, ValueWrapper.ValueType.Int }),
                    new(Constants.OpenPositionFunction.ParameterNames.LeverageType, new [] { ValueWrapper.ValueType.Leverage }),
                    new(Constants.OpenPositionFunction.ParameterNames.Quantity, new [] { ValueWrapper.ValueType.Float, ValueWrapper.ValueType.Int }),
                    new(Constants.OpenPositionFunction.ParameterNames.Cost, new [] { ValueWrapper.ValueType.Float, ValueWrapper.ValueType.Int, ValueWrapper.ValueType.FloatPercent, ValueWrapper.ValueType.IntPercent }),
                    new(Constants.OpenPositionFunction.ParameterNames.Price, new [] { ValueWrapper.ValueType.Float, ValueWrapper.ValueType.Price }),
                    new(Constants.OpenPositionFunction.ParameterNames.Offset, new [] { ValueWrapper.ValueType.Float, ValueWrapper.ValueType.FloatPercent, ValueWrapper.ValueType.IntPercent }),
                    new(Constants.OpenPositionFunction.ParameterNames.Stoploss, new [] { ValueWrapper.ValueType.Float, ValueWrapper.ValueType.FloatPercent, ValueWrapper.ValueType.IntPercent })
                };

                // Require "price","offset" if "order"==limit
                var orderSymbol = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.Order);
                if (orderSymbol != null && orderSymbol.Value.Text.Equals("limit"))
                {
                    var priceSymbol = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.Price);
                    if (priceSymbol == null)
                    {
                        throw new SignalScriptValidationException($"{functionName}() parameter '{Constants.OpenPositionFunction.ParameterNames.Price}' is required when using limit orders");
                    }
                    var offsetSymbol = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.Offset);
                    if (offsetSymbol == null)
                    {
                        throw new SignalScriptValidationException($"{functionName}() parameter '{Constants.OpenPositionFunction.ParameterNames.Offset}' is required when using limit orders");
                    }
                }

                // Must specify exactly one of "cost", "quantity"
                var costSymbol = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.Cost);
                var quantitySymbol = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.Quantity);
                if ((costSymbol == null && quantitySymbol == null) || (costSymbol != null && quantitySymbol != null))
                {
                    throw new SignalScriptValidationException($"{functionName}() requires exactly one of the '{Constants.OpenPositionFunction.ParameterNames.Cost}' or '{Constants.OpenPositionFunction.ParameterNames.Quantity}' parameters");
                }
                break;
            }
            
            default:
                throw new SignalScriptValidationException($"Unrecognised function {functionName}()");
        }

        foreach (var requiredSymbol in requiredSymbols)
        {
            ValidateRequiredParameter(requiredSymbol.Name, requiredSymbol.AllowedTypes, symbolScope, functionName);
        }
        
        foreach (var optionalSymbol in optionalSymbols)
        {
            ValidateOptionalParameter(optionalSymbol.Name, optionalSymbol.AllowedTypes, symbolScope, functionName);
        }

        ValidateUnknownParameters(symbolScope, requiredSymbols, optionalSymbols, functionName);
    }

    private void ValidateRequiredParameter(string parameterName, ValueWrapper.ValueType[] allowedTypes, SymbolScope symbolScope, string functionName)
    {
        var symbol = symbolScope.GetSymbol(parameterName);
        if (symbol == null)
        {
            throw new SignalScriptValidationException($"{functionName}() is missing required parameter '{parameterName}'");
        }

        if (!allowedTypes.Contains(symbol.Value.Type))
        {
            throw new SignalScriptValidationException($"{functionName}() expected parameter '{parameterName}' was of type {symbol.Value.Type}, but expected one of [{string.Join(", ", allowedTypes)}]");
        }

        if (symbol.Value.Type == ValueWrapper.ValueType.String && string.IsNullOrWhiteSpace(symbol.Value.Text))
        {
            throw new SignalScriptValidationException($"{functionName}() parameter '{parameterName}' cannot be empty");
        }
    }

    private void ValidateOptionalParameter(string parameterName, ValueWrapper.ValueType[] allowedTypes, SymbolScope symbolScope, string functionName)
    {
        var symbol = symbolScope.GetSymbol(parameterName);
        if (symbol != null)
        {
            if (!allowedTypes.Contains(symbol.Value.Type))
            {
                throw new SignalScriptValidationException($"{functionName}() expected parameter '{parameterName}' was of type {symbol.Value.Type}, but expected one of [{string.Join(", ", allowedTypes)}]");
            }
            
            if (symbol.Value.Type == ValueWrapper.ValueType.String && string.IsNullOrWhiteSpace(symbol.Value.Text))
            {
                throw new SignalScriptValidationException($"{functionName}() parameter '{parameterName}' cannot be empty");
            }
        }
    }

    private void ValidateUnknownParameters(SymbolScope symbolScope, AllowedSymbol[] requiredSymbols, AllowedSymbol[] optionalSymbols, string functionName)
    {
        HashSet<string> allowedParameters = new ();
        allowedParameters.UnionWith(requiredSymbols.Select(s => s.Name).ToHashSet());
        allowedParameters.UnionWith(optionalSymbols.Select(s => s.Name).ToHashSet());

        foreach (var name in symbolScope.GetNames())
        {
            if (!allowedParameters.Contains(name))
            {
                throw new SignalScriptValidationException($"Unexpected parameter '{name}' found for {functionName}()");
            }
        }
    }

    #endregion
}
