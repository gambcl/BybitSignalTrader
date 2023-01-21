namespace SignalTrader.Data.Attributes;

/// <summary>
/// Attribute used to mark entity fields that should be encrypted before storing in the database.
/// Encryption/decryption is performed using value converters.
/// </summary>
public class ProtectedDataAttribute : Attribute
{
}
