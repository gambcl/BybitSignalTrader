namespace SignalTrader.Common.Docker;

public class DockerSecretsConfigurationProvider : ConfigurationProvider
{
    #region Members

    public const string DefaultSecretsPath = "/run/secrets";
    private readonly string _secretsPath;
    private readonly Action<string> _handle;

    #endregion

    #region Constructors

    public DockerSecretsConfigurationProvider(string secretsPath) : this(secretsPath, null)
    {
    }

    public DockerSecretsConfigurationProvider(string secretsPath, Action<string>? handle)
    {
        _handle = handle ?? (filePath =>
        {
            var fileName = Path.GetFileName(filePath);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var key = fileName.Replace("__", ":");
                var value = File.ReadAllText(filePath).Trim();
                // Console.Out.WriteLine($"Docker Secret [{filePath}] [{key}] = [{value}]");
                    
                Data.Add(key, value);
            }
        });

        _secretsPath = secretsPath ?? throw new ArgumentNullException(nameof(secretsPath));
    }

    #endregion

    #region ConfigurationProvider

    public override void Load()
    {
        if (Directory.Exists(_secretsPath))
        {
            foreach (var file in Directory.EnumerateFiles(_secretsPath))
            {
                _handle(file);
            }
        }
    }

    #endregion
}
