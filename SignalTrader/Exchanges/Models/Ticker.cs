using SignalTrader.Common.Enums;

namespace SignalTrader.Exchanges.Models;

public record Ticker(
    SupportedExchange Exchange,
    string QuoteAsset,
    string BaseAsset,
    string Symbol,
    decimal BidPrice,
    decimal AskPrice,
    decimal LastPrice,
    decimal? VolumeQuote24H,
    decimal? VolumeBase24H
    );
    