using SignalTrader.Authentication.Resources;

namespace SignalTrader.Authentication.Services;

public interface IAuthenticationService
{
    public string? Authenticate(AuthenticateResource resource);
}
