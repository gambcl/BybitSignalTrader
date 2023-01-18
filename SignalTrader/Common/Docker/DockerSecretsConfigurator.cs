namespace SignalTrader.Common.Docker;

public static class DockerSecretsConfigurator
{
    public static IConfigurationBuilder AddDockerSecrets(this IConfigurationBuilder configurationBuilder)
    {
        return AddDockerSecrets(configurationBuilder, DockerSecretsConfigurationProvider.DefaultSecretsPath);
    }

    public static IConfigurationBuilder AddDockerSecrets(this IConfigurationBuilder configurationBuilder, string secretsPath, Action<string>? handle = null)
    {
        configurationBuilder.Add(new DockerSecretsConfigurationSource(secretsPath, handle));
        return configurationBuilder;
    }
}
