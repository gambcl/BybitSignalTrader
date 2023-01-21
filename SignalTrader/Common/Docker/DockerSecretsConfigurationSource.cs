namespace SignalTrader.Common.Docker;

public class DockerSecretsConfigurationSource : IConfigurationSource
{
    #region Members

    private readonly string _secretsPath;
    private readonly Action<string>? _handle;

    #endregion

    #region Constructors

    public DockerSecretsConfigurationSource(string secretsPath) : this(secretsPath, null)
    {
    }

    public DockerSecretsConfigurationSource(string secretsPath, Action<string>? handle)
    {
        _secretsPath = secretsPath ?? throw new ArgumentNullException(nameof(secretsPath));
        _handle = handle;
    }

    #endregion

    #region IConfigurationSource

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DockerSecretsConfigurationProvider(_secretsPath, _handle);
    }

    #endregion
}
