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
                    var pnlEmoji = DeterminePnlEmoji(position.Status, position.RealisedPnlPercent);
                    var sign = pnl.RealisedPnl >= 0.0M ? "+" : "-";
                    detailParts.Add($"Realised P&L: {sign}{Math.Abs(pnl.RealisedPnl)} {position.QuoteAsset} ({sign}{Math.Abs(pnl.RealisedPnlPercent)/100.0M:P2}) {pnlEmoji}");
                }
                // Duration detail.
                detailParts.Add($"Duration: {FormatDuration(position.CreatedUtcMillis, position.CompletedUtcMillis)}");
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

        return new AccuracyResult
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

    private string? DeterminePnlEmoji(PositionStatus status, decimal pnlPercent)
    {
        if (status == PositionStatus.Liquidated)
        {
            return Telegram.Constants.Emojis.SkullCrossBones;
        }
        else if (status == PositionStatus.StopLoss)
        {
            return Telegram.Constants.Emojis.Poo;
        }
        else if (status == PositionStatus.Closed)
        {
            switch (pnlPercent)
            {
                case >= 100.0M:
                    return Telegram.Constants.Emojis.Profit100;
                case >= 90.0M:
                    return Telegram.Constants.Emojis.Profit90;
                case >= 80.0M:
                    return Telegram.Constants.Emojis.Profit80;
                case >= 70.0M:
                    return Telegram.Constants.Emojis.Profit70;
                case >= 60.0M:
                    return Telegram.Constants.Emojis.Profit60;
                case >= 50.0M:
                    return Telegram.Constants.Emojis.Profit50;
                case >= 40.0M:
                    return Telegram.Constants.Emojis.Profit40;
                case >= 30.0M:
                    return Telegram.Constants.Emojis.Profit30;
                case >= 20.0M:
                    return Telegram.Constants.Emojis.Profit20;
                case >= 10.0M:
                    return Telegram.Constants.Emojis.Profit10;
                case >= 1.0M:
                    return Telegram.Constants.Emojis.Profit1;
                case >= 0.0M:
                    return Telegram.Constants.Emojis.Profit0;
                case <= -100.0M:
                    return Telegram.Constants.Emojis.Loss100;
                case <= -90.0M:
                    return Telegram.Constants.Emojis.Loss90;
                case <= -80.0M:
                    return Telegram.Constants.Emojis.Loss80;
                case <= -70.0M:
                    return Telegram.Constants.Emojis.Loss70;
                case <= -60.0M:
                    return Telegram.Constants.Emojis.Loss60;
                case <= -50.0M:
                    return Telegram.Constants.Emojis.Loss50;
                case <= -40.0M:
                    return Telegram.Constants.Emojis.Loss40;
                case <= -30.0M:
                    return Telegram.Constants.Emojis.Loss30;
                case <= -20.0M:
                    return Telegram.Constants.Emojis.Loss20;
                case <= -10.0M:
                    return Telegram.Constants.Emojis.Loss10;
                case < -1.0M:
                    return Telegram.Constants.Emojis.Loss1;
                case < -0.0M:
                    return Telegram.Constants.Emojis.Loss0;
            }
        }

        return null;
    }

    private string FormatDuration(long startUtcMillis, long endUtcMillis)
    {
        string result = string.Empty;

        long durationMillis = Math.Max(startUtcMillis, endUtcMillis) - Math.Min(startUtcMillis, endUtcMillis);
        var remainingTimeSpan = TimeSpan.FromSeconds(durationMillis / 1000.0);
        
        // Format into: ?d ?h ?m
        int days = 0;
        int hours = 0;
        int minutes = 0;

        days = (int)Math.Floor(remainingTimeSpan.TotalDays);
        remainingTimeSpan = remainingTimeSpan - TimeSpan.FromDays(days);

        hours = (int)Math.Floor(remainingTimeSpan.TotalHours);
        remainingTimeSpan = remainingTimeSpan - TimeSpan.FromHours(hours);

        minutes = (int)Math.Floor(remainingTimeSpan.TotalMinutes);
        remainingTimeSpan = remainingTimeSpan - TimeSpan.FromMinutes(minutes);

        /*
        var parts = new List<string>();
        if (days > 0)
        {
            parts.Add(($"{days}d"));
        }
        if (days > 0 || hours > 0)
        {
            parts.Add(($"{hours}h"));
        }
        parts.Add($"{minutes}m");

        return string.Join(" ", parts);
        */
        
        return $"{days}d {hours}h {minutes}m";
    }

    #endregion
}
