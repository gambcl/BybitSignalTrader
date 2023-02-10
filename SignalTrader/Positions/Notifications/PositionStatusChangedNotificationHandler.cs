using MediatR;
using Microsoft.EntityFrameworkCore;
using SignalTrader.Common.Enums;
using SignalTrader.Common.Extensions;
using SignalTrader.Data;
using SignalTrader.Data.Entities;
using SignalTrader.Positions.Models;
using SignalTrader.Positions.Services;
using SignalTrader.Telegram.Services;

namespace SignalTrader.Positions.Notifications;

public class PositionStatusChangedNotificationHandler : INotificationHandler<PositionStatusChangedNotification>
{
    #region Members

    private readonly ILogger<PositionStatusChangedNotificationHandler> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    #endregion

    #region Constructors

    public PositionStatusChangedNotificationHandler(ILogger<PositionStatusChangedNotificationHandler> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    #endregion
    
    #region INotificationHandler<PositionStatusChangedNotification>

    public async Task Handle(PositionStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();
            var signalTraderDbContext = scope.ServiceProvider.GetRequiredService<SignalTraderDbContext>();
            var positionsService = scope.ServiceProvider.GetRequiredService<IPositionsService>();

            var position = notification.Position;

            if (position.IsComplete)
            {
                // Update PnL in db.
                _logger.LogDebug("Calculating PnL for {Exchange}:{Base}{Quote}", position.Exchange, position.BaseAsset, position.QuoteAsset);
                var pnl = await positionsService.CalculateProfitAndLossAsync(position);
                if (pnl.Success)
                {
                    position.UnrealisedPnl = pnl.UnrealisedPnl;
                    position.UnrealisedPnlPercent = pnl.UnrealisedPnlPercent;
                    position.RealisedPnl = pnl.RealisedPnl;
                    position.RealisedPnlPercent = pnl.RealisedPnlPercent;
                }
                else
                {
                    _logger.LogError("Failed to calculate PnL for {Exchange}:{Base}{Quote}: {Error}", position.Exchange, position.BaseAsset, position.QuoteAsset, pnl.Message);
                }

                var unfilled = (pnl.QuantityFilled == 0.0M ? "unfilled " : string.Empty);
                var detailParts = new List<string>();
                if (pnl.QuantityFilled > 0.0M)
                {
                    // P&L detail.
                    var pnlEmoji = PositionsService.DeterminePnlEmoji(position.Status, position.RealisedPnlPercent);
                    var sign = pnl.RealisedPnl >= 0.0M ? "+" : "-";
                    detailParts.Add($"Realised P&L: {sign}{Math.Abs(pnl.RealisedPnl)} {position.QuoteAsset} ({sign}{Math.Abs(pnl.RealisedPnlPercent)/100.0M:P2}) {pnlEmoji}");
                }
                // Duration detail.
                detailParts.Add($"Duration: {PositionsService.FormatDuration(position.CreatedUtcMillis, position.CompletedUtcMillis)}");
                // Accuracy detail.
                if (pnl.QuantityFilled > 0.0M)
                {
                    var accuracy = await CalculateAccuracyAsync(position, signalTraderDbContext);
                    var winnersLosers = new List<string>();
                    if (accuracy.NumberWinners > 0)
                    {
                        winnersLosers.Add($"{accuracy.NumberWinners} {("winner".Pluralize(accuracy.NumberWinners))}");
                    }
                    if (accuracy.NumberLosers > 0)
                    {
                        winnersLosers.Add($"{accuracy.NumberLosers} {("loser".Pluralize(accuracy.NumberLosers))}");
                    }
                    detailParts.Add($"Accuracy: {accuracy.Accuracy/100.0M:P1} ({string.Join(", ", winnersLosers)})");
                }
                var detail = string.Join("\n", detailParts);

                if (position.Status == PositionStatus.Closed)
                {
                    // Send notifications.
                    await telegramService.SendMessageNotificationAsync(
                        position.Direction.ToEmoji(),
                        position.Account.Name,
                        $"Closed {unfilled}{position.Direction} position of {position.Quantity} {position.BaseAsset}{position.QuoteAsset}",
                        detail);
                }
                else if (position.Status == PositionStatus.StopLoss)
                {
                    // Send notifications.
                    await telegramService.SendMessageNotificationAsync(
                        position.Direction.ToEmoji(),
                        position.Account.Name,
                        $"{position.Direction} position of {position.Quantity} {position.BaseAsset}{position.QuoteAsset} was stopped out",
                        detail);
                }
                else if (position.Status == PositionStatus.Liquidated)
                {
                    // Send notifications.
                    await telegramService.SendMessageNotificationAsync(
                        position.Direction.ToEmoji(),
                        position.Account.Name,
                        $"{position.Direction} position of {position.Quantity} {position.BaseAsset}{position.QuoteAsset} has been liquidated",
                        detail);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in Handle<PositionStatusChangedNotification>");
        }
    }

    #endregion

    #region Private

    private async Task<AccuracyResult> CalculateAccuracyAsync(Position position, SignalTraderDbContext signalTraderDbContext)
    {
        const int percentDecimalPlaces = 4;
        decimal accuracy = 0.0M;
        int filledClosedPositionsCount = 0;
        int winnersCount = 0;
        
        // Get all closed positions matching account/quote/base.
        var closedPositions = await signalTraderDbContext.Positions
            .Include(p => p.Orders)
            .Where(p => p.AccountId == position.AccountId &&
                        p.QuoteAsset == position.QuoteAsset &&
                        p.BaseAsset == position.BaseAsset &&
                        (p.Status == PositionStatus.Closed || p.Status == PositionStatus.StopLoss || p.Status == PositionStatus.Liquidated))
            .AsNoTracking()
            .ToListAsync();

        // Only interested in filled positions.
        var filledClosedPositions = new List<Position>();
        foreach (var closedPosition in closedPositions)
        {
            Side entrySide = closedPosition.Direction == Direction.Long ? Side.Buy : Side.Sell;
            var quantityFilled = closedPosition.Orders.Where(o => o.Side == entrySide).Sum(o => o.QuantityFilled);
            if (quantityFilled > 0.0M)
            {
                filledClosedPositions.Add(closedPosition);
            }
        }

        filledClosedPositionsCount = filledClosedPositions.Count;
        if (filledClosedPositionsCount > 0)
        {
            // Calculate percentage of profitable positions.
            var profitableClosedPositions = filledClosedPositions.FindAll(p => p.RealisedPnl >= 0.0M).ToList();
            winnersCount = profitableClosedPositions.Count;
            accuracy = ((decimal)winnersCount / filledClosedPositionsCount) * 100.0M;
        }

        return new AccuracyResult(true)
        {
            Exchange = position.Exchange,
            QuoteAsset = position.QuoteAsset,
            BaseAsset = position.BaseAsset,
            Accuracy = accuracy.TruncateToDecimalPlaces(percentDecimalPlaces),
            NumberPositions = filledClosedPositionsCount,
            NumberWinners = winnersCount,
            NumberLosers = filledClosedPositionsCount - winnersCount
        };
    }

    #endregion
}
