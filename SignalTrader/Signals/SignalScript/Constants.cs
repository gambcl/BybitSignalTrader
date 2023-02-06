namespace SignalTrader.Signals.SignalScript;

public static class Constants
{
    public static class FunctionNames
    {
        public const string Signal = "signal";
        public const string CancelOrders = "cancelOrders";
        public const string ClosePosition = "closePosition";
        public const string OpenPosition = "openPosition";
    }
    
    public static class SignalFunction
    {
        public static class ParameterNames
        {
            public const string StrategyName = "strategyName";
            public const string SignalName = "signalName";
            public const string Exchange = "exchange";
            public const string Ticker = "ticker";
            public const string BaseAsset = "baseAsset";
            public const string QuoteAsset = "quoteAsset";
            public const string Interval = "interval";
            public const string SignalTime = "signalTime";
            public const string BarTime = "barTime";
            public const string Open = "open";
            public const string High = "high";
            public const string Low = "low";
            public const string Close = "close";
            public const string Volume = "volume";
            public const string Passphrase = "passphrase";
            public const string LongEnabled = "longEnabled";
            public const string ShortEnabled = "shortEnabled";
        }
    }
    
    public static class AccountFunction
    {
        public static class ParameterNames
        {
            public const string AccountId = "accountId";
            public const string BaseAsset = "baseAsset";
            public const string QuoteAsset = "quoteAsset";
        }
    }
    
    public static class CancelOrdersFunction
    {
        public static class ParameterNames
        {
            public const string AccountId = "accountId";
            public const string BaseAsset = "baseAsset";
            public const string QuoteAsset = "quoteAsset";
            public const string Side = "side";
        }
    }
    
    public static class ClosePositionFunction
    {
        public static class ParameterNames
        {
            public const string AccountId = "accountId";
            public const string BaseAsset = "baseAsset";
            public const string QuoteAsset = "quoteAsset";
            public const string Direction = "direction";
            public const string Order = "order";
            public const string Price = "price";
            public const string Offset = "offset";
        }
    }
    
    public static class OpenPositionFunction
    {
        public static class ParameterNames
        {
            public const string AccountId = "accountId";
            public const string BaseAsset = "baseAsset";
            public const string QuoteAsset = "quoteAsset";
            public const string Direction = "direction";
            public const string Order = "order";
            public const string Price = "price";
            public const string Offset = "offset";
            public const string Leverage = "leverage";
            public const string LeverageType = "leverageType";
            public const string Quantity = "quantity";
            public const string Cost = "cost";
            public const string StopLoss = "stopLoss";
        }
    }
}
