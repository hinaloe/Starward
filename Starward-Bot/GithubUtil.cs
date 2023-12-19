using Octokit;

namespace Starward_Bot;

internal static class GithubUtil
{

    public static async Task<GitHubClient> CreateGithubClient()
    {
        var generator = new GitHubJwt.GitHubJwtFactory(
                new GitHubJwt.StringPrivateKeySource(Environment.GetEnvironmentVariable("GITHUB_PEM")),
                new GitHubJwt.GitHubJwtFactoryOptions
                {
                    AppIntegrationId = 374100,
                    ExpirationSeconds = 600
                });
        var jwtToken = generator.CreateEncodedJwtToken();
        var appClient = new GitHubClient(new ProductHeaderValue("Starward-Bot"))
        {
            Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
        };

        var installation = await appClient!.GitHubApps.GetRepositoryInstallationForCurrent("Scighost", "Starward");
        var response = await appClient.GitHubApps.CreateInstallationToken(installation.Id);

        var installationClient = new GitHubClient(new ProductHeaderValue("Starward-Bot"))
        {
            Credentials = new Credentials(response.Token)
        };
        return installationClient;
    }

}
