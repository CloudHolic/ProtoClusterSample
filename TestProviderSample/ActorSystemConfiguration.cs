using Proto;
using Proto.Cluster;
using Proto.Cluster.Testing;
using Proto.Cluster.Partition;
using Proto.Remote.GrpcNet;

namespace TestProviderSample;

public static class ActorSystemConfiguration
{
    public static void AddActorSystem(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddSingleton(provider =>
        {
            var actorSystemConfig = ActorSystemConfig.Setup();

            var remoteConfig = GrpcNetRemoteConfig.BindToLocalhost();

            var clusterConfig = ClusterConfig
                .Setup(
                    "TestProviderSampleCluster",
                    new TestProvider(new TestProviderOptions(), new InMemAgent()),
                    new PartitionIdentityLookup())
                .WithClusterKind(
                    "user",
                    Props.FromProducer(() => new UserGrainActor()));

            return new ActorSystem(actorSystemConfig)
                .WithRemote(remoteConfig)
                .WithCluster(clusterConfig);
        });
    }
}
