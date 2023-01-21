using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SignalTrader.Accounts.Resources;
using SignalTrader.Common.Enums;
using SignalTrader.Data.Attributes;

namespace SignalTrader.Data.Entities;

[Index(nameof(Exchange))]
[Index(nameof(QuoteAsset))]
[Index(nameof(AccountType))]
public class Account
{
    public Account()
    {
        AccountType = AccountType.Live;
        CreatedUtcMillis = UpdatedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
        
    [Key]
    public long Id { get; set; }
        
    [ProtectedData]
    [MinLength(1), MaxLength(100)]
    [Column(TypeName = "varchar(500)")]
    public string Name { get; set; } = null!;
        
    [ProtectedData]
    [MaxLength(1000)]
    [Column(TypeName = "varchar(4000)")]
    public string? Comment { get; set; }

    [MinLength(1), MaxLength(50)]
    public SupportedExchange Exchange { get; set; }
    
    [MinLength(1), MaxLength(50)]
    public string QuoteAsset { get; set; } = null!;
        
    public AccountType AccountType { get; set; }
        
    [ProtectedData]
    [MaxLength(500)]
    [Column(TypeName = "varchar(2000)")]
    public string? ApiKey { get; set; }
        
    [ProtectedData]
    [MaxLength(500)]
    [Column(TypeName = "varchar(2000)")]
    public string? ApiSecret { get; set; }
        
    [ProtectedData]
    [MaxLength(500)]
    [Column(TypeName = "varchar(2000)")]
    public string? ApiPassphrase { get; set; }
        
    public long CreatedUtcMillis { get; set; }

    [NotMapped]
    public string CreatedUtcIso8601 => DateTimeOffset.FromUnixTimeMilliseconds(CreatedUtcMillis).ToString("O");

    public long UpdatedUtcMillis { get; set; }

    [NotMapped]
    public string UpdatedUtcIso8601 => DateTimeOffset.FromUnixTimeMilliseconds(UpdatedUtcMillis).ToString("O");

    public AccountResource ToAccountResource()
    {
        return new AccountResource
        {
            Id = Id,
            Name = Name,
            Comment = Comment,
            Exchange = Exchange,
            QuoteAsset = QuoteAsset,
            AccountType = AccountType,
            ApiKey = ApiKey,
            CreatedUtcMillis = CreatedUtcMillis,
            UpdatedUtcMillis = UpdatedUtcMillis
        };
    }
}
