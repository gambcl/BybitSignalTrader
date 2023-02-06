using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SignalTrader.Common.Enums;
using SignalTrader.Data.Attributes;
using SignalTrader.Data.Entities;

namespace SignalTrader.Data;

public class SignalTraderDbContext : DbContext
{
    #region Constants

    public const string DefaultSchema = "signaltrader";
    private const string DataProtectionPurpose = "ProtectedData";

    #endregion

    #region Constructors

    public SignalTraderDbContext(DbContextOptions<SignalTraderDbContext> options) : base(options)
    {
    }

    #endregion

    #region DbContext

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        
        // Account - ProtectedData
        modelBuilder.Entity<Account>(b =>
        {
            this.AddProtectedDataValueConverters(b);
        });
        // Account - Enums
        modelBuilder.Entity<Account>()
            .Property(a => a.Exchange)
            .HasConversion(
                v => v.ToString(),
                v => (SupportedExchange)Enum.Parse(typeof(SupportedExchange), v));
        modelBuilder.Entity<Account>()
            .Property(a => a.AccountType)
            .HasConversion(
                v => v.ToString(),
                v => (AccountType)Enum.Parse(typeof(AccountType), v));
        modelBuilder.Entity<Account>()
            .Property(a => a.ExchangeType)
            .HasConversion(
                v => v.ToString(),
                v => (ExchangeType)Enum.Parse(typeof(ExchangeType), v));

        // Position - Enums
        modelBuilder.Entity<Position>()
            .Property(p => p.Exchange)
            .HasConversion(
                v => v.ToString(),
                v => (SupportedExchange)Enum.Parse(typeof(SupportedExchange), v));
        modelBuilder.Entity<Position>()
            .Property(p => p.Direction)
            .HasConversion(
                v => v.ToString(),
                v => (Direction)Enum.Parse(typeof(Direction), v));
        modelBuilder.Entity<Position>()
            .Property(p => p.LeverageType)
            .HasConversion(
                v => v.ToString(),
                v => (LeverageType)Enum.Parse(typeof(LeverageType), v));
        modelBuilder.Entity<Position>()
            .Property(p => p.Status)
            .HasConversion(
                v => v.ToString(),
                v => (PositionStatus)Enum.Parse(typeof(PositionStatus), v));
        // Position - Relationships
        modelBuilder.Entity<Position>()
            .HasOne(p => p.Account)
            .WithMany(a => a.Positions)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Order - Enums
        modelBuilder.Entity<Order>()
            .Property(o => o.Exchange)
            .HasConversion(
                v => v.ToString(),
                v => (SupportedExchange)Enum.Parse(typeof(SupportedExchange), v));
        modelBuilder.Entity<Order>()
            .Property(o => o.Side)
            .HasConversion(
                v => v.ToString(),
                v => (Side)Enum.Parse(typeof(Side), v));
        modelBuilder.Entity<Order>()
            .Property(o => o.Type)
            .HasConversion(
                v => v.ToString(),
                v => (OrderType)Enum.Parse(typeof(OrderType), v));
        modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion(
                v => v.ToString(),
                v => (OrderStatus)Enum.Parse(typeof(OrderStatus), v));
        // Order - Relationships
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Position)
            .WithMany(p => p.Orders)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Account)
            .WithMany(a => a.Orders)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Signal> Signals { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    #endregion
    
    #region Private

    /// <summary>
    /// ValueConverter for strings.
    /// </summary>
    private class ProtectedDataConverter : ValueConverter<string, string>
    {
        public ProtectedDataConverter(IDataProtectionProvider protectionProvider)
            : base(
                s => protectionProvider
                    .CreateProtector(DataProtectionPurpose)
                    .Protect(s),
                s => protectionProvider
                    .CreateProtector(DataProtectionPurpose)
                    .Unprotect(s),
                default)
        {
        }
    }

    /// <summary>
    /// ValueConverter for non-strings.
    /// </summary>
    private class ProtectedDataConverter<T> : ValueConverter<T, string>
    {
        public ProtectedDataConverter(IDataProtectionProvider protectionProvider)
            : base(
                s => protectionProvider
                    .CreateProtector(DataProtectionPurpose)
                    .Protect(JsonSerializer.Serialize<T>(s, (JsonSerializerOptions?)default)),
                s => JsonSerializer.Deserialize<T>(
                    protectionProvider.CreateProtector(DataProtectionPurpose).Unprotect(s),
                    (JsonSerializerOptions?)default)!,
                default)
        {
        }
    }
    
    private void AddProtectedDataValueConverters<TEntity>(EntityTypeBuilder<TEntity> b)
        where TEntity : class
    {
        var protectedProps = typeof(TEntity).GetProperties()
            .Where(prop => Attribute.IsDefined(prop, typeof(ProtectedDataAttribute)));

        foreach (var p in protectedProps)
        {
            if (p.PropertyType != typeof(string))
            {
                // You could throw a NotSupportedException here if you only care about strings
                var converterType = typeof(ProtectedDataConverter<>)
                    .MakeGenericType(p.PropertyType);
                var converter = (ValueConverter)Activator
                    .CreateInstance(converterType, this.GetService<IDataProtectionProvider>())!;

                b.Property(p.PropertyType, p.Name).HasConversion(converter);
            }
            else
            {
                ProtectedDataConverter converter = new ProtectedDataConverter(
                    this.GetService<IDataProtectionProvider>());

                b.Property(typeof(string), p.Name).HasConversion(converter);
            }
        }
        
    }

    #endregion
}
