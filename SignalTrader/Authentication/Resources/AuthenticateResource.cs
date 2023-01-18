using System.ComponentModel.DataAnnotations;

namespace SignalTrader.Authentication.Resources;

public record AuthenticateResource([Required] string PasswordBase64);
