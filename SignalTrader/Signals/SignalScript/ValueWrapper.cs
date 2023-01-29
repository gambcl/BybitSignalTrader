using SignalTrader.Common.Enums;
using SignalTrader.Signals.SignalScript.Exceptions;

namespace SignalTrader.Signals.SignalScript;

public class ValueWrapper
{
    #region Enums

    public enum ValueType
    {
        String,
        Side,
        Direction,
        Price,
        Order,
        Leverage,
        Boolean,
        Float,
        FloatPercent,
        Int,
        IntPercent
    }

    #endregion

    #region Constructors

    public ValueWrapper(ValueType type, string text)
    {
        Type = type;
        Text = text;
    }

    #endregion

    #region Properties

    public ValueType Type { get; }
    public string Text { get; }

    #endregion

    #region Public

    public string GetStringValue()
    {
        return Text;
    }

    public int GetIntValue()
    {
        switch (Type)
        {
            case ValueType.Int:
                return Int32.Parse(Text);
            case ValueType.IntPercent:
                return Int32.Parse(Text[..^1]);
            default:
                throw new ValueWrapperException($"Attempted to get {Type} value as Int");
        }
    }
    
    public decimal GetDecimalValue()
    {
        switch (Type)
        {
            case ValueType.Float:
                return Decimal.Parse(Text);
            case ValueType.FloatPercent:
                return Decimal.Parse(Text[..^1]);
            case ValueType.Int:
                return Decimal.Parse(Text);
            case ValueType.IntPercent:
                return Decimal.Parse(Text[..^1]);
            default:
                throw new ValueWrapperException($"Attempted to get {Type} value as Decimal");
        }
    }
    
    public bool GetBooleanValue()
    {
        switch (Type)
        {
            case ValueType.Int:
                return Int32.Parse(Text) != 0;
            case ValueType.Boolean:
                return Boolean.Parse(Text);
            default:
                throw new ValueWrapperException($"Attempted to get {Type} value as Boolean");
        }
    }
    
    public Side GetSideValue()
    {
        switch (Type)
        {
            case ValueType.Side:
                return Enum.Parse<Side>(Text, true);
            default:
                throw new ValueWrapperException($"Attempted to get {Type} value as Side");
        }
    }
    
    public Direction GetDirectionValue()
    {
        switch (Type)
        {
            case ValueType.Direction:
                return Enum.Parse<Direction>(Text, true);
            default:
                throw new ValueWrapperException($"Attempted to get {Type} value as Direction");
        }
    }
    
    public Price GetPriceValue()
    {
        switch (Type)
        {
            case ValueType.Price:
                return Enum.Parse<Price>(Text, true);
            default:
                throw new ValueWrapperException($"Attempted to get {Type} value as Price");
        }
    }
    
    public OrderType GetOrderValue()
    {
        switch (Type)
        {
            case ValueType.Order:
                return Enum.Parse<OrderType>(Text, true);
            default:
                throw new ValueWrapperException($"Attempted to get {Type} value as Order");
        }
    }
    
    public LeverageType GetLeverageValue()
    {
        switch (Type)
        {
            case ValueType.Leverage:
                return Enum.Parse<LeverageType>(Text, true);
            default:
                throw new ValueWrapperException($"Attempted to get {Type} value as Leverage");
        }
    }
    
    public override string ToString()
    {
        return $"<{Type}:{Text}>";
    }

    #endregion
}
