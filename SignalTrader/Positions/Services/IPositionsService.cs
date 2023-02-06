using SignalTrader.Common.Enums;
using SignalTrader.Data.Entities;
using SignalTrader.Positions.Models;
using SignalTrader.Signals.SignalScript;

namespace SignalTrader.Positions.Services;

public interface IPositionsService
{
    public Task OpenPositionAsync(long? accountId, string? quoteAsset, string? baseAsset, decimal? leverageMultiplier, LeverageType? leverageType, Direction? direction, OrderType? orderType, decimal? quantity, ValueWrapper? costValue, ValueWrapper? priceValue, ValueWrapper? offsetValue, ValueWrapper? stopLossValue);
    public Task ClosePositionAsync(long? accountId, string? quoteAsset, string? baseAsset, Direction? direction, OrderType? orderType, ValueWrapper? priceValue, ValueWrapper? offsetValue);

    public Task UpdatePositionsAsync();
    public Task UpdatePositionAsync(Position position);
    public Task<ProfitAndLossResult> CalculateProfitAndLossAsync(Position position);
}
