namespace SignalTrader.Accounts.Models;

public record AccountWalletBalance(string Asset, decimal WalletBalance, decimal AvailableBalance);
