using Proto;
using Proto.Cluster;
using Proto.Cluster.Partition;
using Proto.Remote.GrpcNet;
using K8sProviderSample.Grains;
using Proto.Cluster.Kubernetes;
using Proto.Remote;

namespace K8sProviderSample;

public static class ActorSystemConfiguration
{
    public static void AddActorSystem(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddSingleton(provider =>
        {
            var actorSystemConfig = ActorSystemConfig.Setup();

            var remoteConfig = GrpcNetRemoteConfig
                .BindToAllInterfaces(configuration["ProtoActor:AdvertisedHost"])
                .WithProtoMessages(SmartBulbMessagesReflection.Descriptor);

            var clusterConfig = ClusterConfig
                .Setup(
                    "TestProviderSampleCluster",
                    new KubernetesProvider(),
                    new PartitionIdentityLookup())
                .WithClusterKind(
                    SmartBulbGrainActor.Kind,
                    Props.FromProducer(() =>
                        new SmartBulbGrainActor((context, clusterIdentity) => new SmartBulbGrain(context, clusterIdentity))))
                .WithClusterKind(
                    SmartHouseGrainActor.Kind,
                    Props.FromProducer(() =>
                        new SmartHouseGrainActor((context, clusterIdentity) => new SmartHouseGrain(context, clusterIdentity))));

            return new ActorSystem(actorSystemConfig)
                .WithRemote(remoteConfig)
                .WithCluster(clusterConfig);
        });
    }
}