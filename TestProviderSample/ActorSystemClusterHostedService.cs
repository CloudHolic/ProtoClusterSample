using Proto;
using Proto.Cluster;

namespace TestProviderSample;

public class ActorSystemClusterHostedService : IHostedService
{
    private readonly ActorSystem _actorSystem;

    public ActorSystemClusterHostedService(ActorSystem actorSystem)
    {
        _actorSystem = actorSystem;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _actorSystem.Cluster().StartMemberAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _actorSystem.Cluster().ShutdownAsync();
    }
}
