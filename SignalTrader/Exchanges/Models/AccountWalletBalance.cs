namespace SignalTrader.Exchanges.Models;

public record AccountWalletBalance(string Asset, decimal WalletBalance, decimal AvailableBalance);
