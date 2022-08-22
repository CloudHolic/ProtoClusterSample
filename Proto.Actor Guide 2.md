## Proto.Actor 가이드 2 - Cluster

### 1. Grains

- Grain = Virtual Actor
- Cluster라는 거대한 집합체에 가상에 Actor가 속해 있는 듯한 구조
- ![Structure](https://raw.githubusercontent.com/CloudHolic/ProtoClusterSample/master/Images/GrainStructure.webp)

- Actor와의 주요 차이점
  - PID 대신 Cluster identity를 통해 각각의 Grain을 구별함
    - Cluster identity = kind + identity (ex. ```vehicle/123```, ```user/2```, etc.)
  - Actor와는 다르게 자동적으로 만들어지며, 사용자가 아닌 Cluster가 이를 관리함
    - 가상의 Actor가 있는 것으로 간주하고 요청을 보내면 Cluster에서 실제로 만들어줘서 처리하는 구조
  - Grain이 실제론 어느 Cluster에 속해있는지 알 수 없음
    - 동일한 Cluster identity를 가진 Grain을 서로 다른 Cluster Member에서 처리할 수도 있음
  - Send를 사용하지 않음
    - 반드시 Request / Response만을 사용하여 요청



### 2.  Grain Lifecycle

- ![Lifecycle1](https://github.com/CloudHolic/ProtoClusterSample/blob/master/Images/Lifecycle1.png?raw=true)
  - 서비스 시작 시점
  - Client가 ```user/123```이라는 Grain에게 메시지를 보내고 싶어하는 상황
  - ```Member1```, ```Member2```, ```Member3``` - Cluster에 속한 Member
  - 실제 Actor는 아직 어디에도 생성되지 않음
- ![Lifecycle2](https://github.com/CloudHolic/ProtoClusterSample/blob/master/Images/Lifecycle2.png?raw=true)
  - 이 요청을 처리할 Cluster Member를 고름
    - 이 예시에서는 ```Member2```
  - 선택된 Cluster인 ```Member2```에서 그제서야 실제 Actor를 생성하고 PID를 부여함
- ![Lifecycle3](https://github.com/CloudHolic/ProtoClusterSample/blob/master/Images/Lifecycle3.png?raw=true)
  - 만일 ```Member2```가 다운되어 기능을 수행할 수 없을 경우, 동일한 역할을 하는 Actor가 다른 Member에서 생성됨
    - PID는 달라지지만 Cluster identity는 동일
- Grain은 Cluster에 의해 자동적으로 생성되지만 자동적으로 종료시킬 수 없음
  - 명시적인 메시지를 받으면 자동적으로 종료되게 설정
    - 종료를 암시하는 특정한 메시지가 존재하는 서비스의 경우
  - ```ReceiveTimeout```을 설정한 후 발생하는 ```Timeout``` 메시지에서 자기 자신을 posioning



### 3. Proto.Cluster

- Proto.Actor에서 Cluster 및 Grain 개념을 지원하기 위해 제공하는 프레임워크

- 필요한 Nuget 패키지
  - ```Grpc.Tools```
  - ```Proto.Actor```
  - ```Proto.Cluster```
  - ```Proto.Cluster.Codegen```
  - ```Proto.Cluster.TestProvider```
  - ```Proto.Remote```

- ```csharp
  using Proto;
  using Proto.Cluster;
  using Proto.Remote.GrpcNet;
  
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
  
  var actorSystem = new ActorSystem(actorSystemConfig)
      .WithRemote(remoteConfig)
      .WithCluster(clusterConfig)
  ```

  - Cluster를 사용하기 위해서는 Remote Config, Cluster Config도 같이 설정해야 함
  - ClusterConfig에서 지정하는 옵션
    - Cluster Name
    - Cluster Provider - Cluster member의 위치 및 그 정보를 제공
      - Test Provider (Local)
      - Consul Provider
      - Kubernetes Provider
      - Amazon ECS Provider
      - IClusterProvider를 직접 구현하여 만든 Custom Provider
    - Identity Lookup - Cluster가 실제 Actor를 찾는 방법
      - Partition Identity Lookup
        - Actor가 메모리에 저장되며 Partition 단위로 분할
        - 각각의 Cluster member는 하나의 Partition의 권한을 가짐
        - 대부분의 상황에서 사용
      - DB Identity Lookup
        - 모든 Actor가 외부 DB에 저장됨
      - Partition Activator Lookup
        - Partition Identity Lookup과 동일한 구조를 가짐
        - Consistent Hashing 기반으로 Actor의 위치를 특정
        - Experimental
  - Grain은 Cluster에 의해 자동적으로 생성되므로 Cluster Config를 설정할 때 만들 Grain kind도 같이 지정

- ```csharp
  using Proto;
  using Proto.Cluster;
  
  var cluster = actorSystem.Cluster();
  /* -- */
  var cluster = context.Cluster();
  ```

  - Cluster는 ```ActorSystem``` 혹은 ```IContext```에서 접근 가능

- ```csharp
  await actorSystem.Cluster().StartMemberAsync();
  /* -- */
  await actorSystem.Cluster().StartClientAsync();
  /* -- */
  await actorSystem.Cluster().ShutdownAsync();
  ```

  - ```StartMemberAsync``` - Cluster Member를 생성
  - ```StartClientAsync``` - Cluster에 요청을 보내기 위한 Client 생성
  - ```ShutdownAsync``` - Cluster 종료

- ```csharp
  var response = await cluster.RequestAsync<BlockUserResponse>(
  	"150", "user", new BlockUser(),
      CancellationTokens.WithTimeout(TimeSpan.FromSeconds(1)));
  ```

  - Cluster에 메시지를 보내는 방법
  - Reqest Timeout 발생시 null이 리턴됨

- ASP.NET의 경우

  - ```csharp
    services.AddSingleton(provider =>
    {
        var actorSystemConfig = ActorSystemConfig.Setup();
        var remoteConfig = GrpcNetRemoteConfig.BindTo(/* .. */);
        var clusterConfig = ClusterConfig.Setup(/* .. */);
        
        return new ActorSystem(actorSystemConfig)
            .WithServiceProvider(provider)
            .WithRemote(remoteConfig)
            .WithClusterConfig(clusterConfig);
    });
    ```

  - ```csharp
    public class ActorSystsemClusterHostedService : IHostedService
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
    ```



### 4. Grains

- ```Proto.Cluster.Codegen``` 패키지를 통해 ```.proto``` 파일로 Grain Boilerplate를 만들 수 있음

- ```.proto``` 문법은 [여기](https://developers.google.com/protocol-buffers/docs/proto3)를 참조

- ```protobuf
  syntax = "proto3";
  
  option csharp_namespace = "...";
  
  import "CounterGrainMessages.proto";
  import "google/protobuf/empty.proto";
  
  service CounterGrain
  {
      rpc Increment (google.protobuf.Empty) returns (google.protobuf.Empty);
      rpc GetCurrentValue (google.protobuf.Empty) returns (CounterValue);
  }
  ```

- ```protobuf
  syntax = "proto3";
  
  option csharp_namespace = "...";
  
  message CounterValue
  {
      int32 Value = 1;
  }
  ```

- ```xml
  <ItemGroup>
      <Protobuf Include="CounterGrainMessages.proto"/>
      <ProtoGrain Include="CounterGrain.proto" AdditionalImportDirs="."/>
  </ItemGroup>
  ```

  - 그 후 ```csproj``` 파일에 해당 파일을 ```ProtoGrain```으로 지정

- 아래의 3가지 파일이 자동적으로 생성됨

  - ```<grain>Base.cs``` - abstract class
  - ```<grain>Actor.cs``` - 실제 Actor class
  - ```<grain>Client.cs``` - Grain과 통신하기 위한 class

- ```csharp
  using Proto;
  
  public class CounterGrain : CounterGrainBase
  {
      private int _value = 0;
      
      public CounterGrain(IContext context) : base(context)
      {
          
      }
      
      public override Task Increment()
      {
          _value++;
          return Task.CompletedTask;
      }
      
      public override Task<CounterValue> GetCurrentValue()
      {
          return Task.FromResult(new CounterValue { Value = _value });
      }
  }
  ```

  - 그 후 ```<grain>Base``` 클래스를 마저 구현
  - 실제로 사용하기 위해서는 ```<grain>Actor``` 클래스에 감싸서 사용

- ```csharp
  using Proto;
  using Proto.Cluster;
  
  var clusterConfig = ClusterConfig
      .Setup(/* .. */)
      .WithClusterKind(
  		CounterGrainActor.Kind,
  		Props.FromProducer(() =>
  			new CounterGrainActor((context, clusterIdentity) => new CounterGrain(context))));
  
  /* .. */
  using Microsoft.Extensions.DependencyInjection;
  
  var clusterConfig = ClusterConfig
      .Setup(/* .. */)
      .WithClusterKind(
  		CounterGrainActor.Kind,
  		Props.FromProducer(() =>
  			new CounterGrainActor((context, clusterIdentity) =>
  				ActivatorUtilities.CreateInstance<CounterGrain>(provider, context))));
  ```

  - ```Kind``` 역시 미리 정의되어 있음

- ``` csharp
  public override Task Increment()
  {
      _value++;
      
      var identity = Context.ClusterIdentity()!;
      Console.WriteLine($"Incrementing {identity.Kind} / {identity.Identity}");
      
      return Task.CompletedTask;
  }
  ```

  - ```IContext```는 ```<grain>Base``` 클래스에 이미 정의되어 있어 Grain 클래스 내부에서 그대로 사용 가능
  - ```IContext```로 Kind와 Identity 역시 접근 가능

- ```csharp
  public override Task OnStarted()
  {
      Console.WriteLine("Starting counter");
      return base.OnStarted();
  }
  public override Task OnStopping()
  {
      Console.WriteLine("Stopping counter");
      return base.OnStopping();
  }
  public override Task OnStopped()
  {
      Console.WriteLine("Stopped counter");
      return base.OnStopped();
  }
  ```

  - LifeCycle에 관련된 메시지는 ```<grain>Base```에 정의된 관련 함수들을 override해서 받을 수 있음

- ```csharp
  public override async Task OnReceive()
  {
      /* .. */
  }
  ```

  - ```.proto```에 정의되지 않은 메시지를 받아야 할 경우 ```OnReceive```를 override하여 처리 가능

- ```csharp
  var client = cluster.GetCounterGrain("click-counter");
  var client = context.GetCounterGrain("click-counter");
  /* .. */
  var incrementResponse = await client.Increment(CancellationTokens.FromSeconds(1));
  var currentValueResponse = await client.GetCurrentValue(CancellationTokens.FromSeconds(1));
  ```

  - Client의 타입은 ```<grain>Client``` 
  - cluster 혹은 context에서 접근 가능
  - 구현한 ```<grain>``` 클래스의 메소드를 Client 클래스에서 그대로 사용 가능



### 5. Publish

- ![PubSub](D:\Projects\ProtoClusterSample\Images\PubSub.png)

  - 메시지를 broadcasting하여 subscribe를 신청한 모든 클러스터들에게 메시지를 보내는 시스템
  - 이러한 메시지를 **Topic**이라고 부름.

- ```csharp
  var publisher = context.Cluster().Publisher();
  
  await publisher.Publish("topic", new ChatMessage { Message = "Hello" });
  
  await publisher.PublishBatch("topic", ChatTopic, new ChatMessage[]
  {
      new() { Message = "Hello" },
      new() { Message = "world" }
  });
  ```

  - ```IPublisher``` 를 통해 topic을 publishing함

- ```csharp
  var pid = await cluster.Subscribe("topic", context =>
  {
      if (context.Message is ChatMessage)
      {
          // process
      }
      
      return Task.CompletedTask;
  });
  ```

  - Topic을 구독해서 처리할 새 Actor를 생성

- ```csharp
  await cluster.Subscribe("topic", pid);
  ```

  - 기존 Actor에서 메시지를 구독 및 처리

- ```csharp
  public class User : UserActorBase
  {
      private const string ChatTopic = "chat";
      
      public User(IContext context) : base(context)
      {
          
      }
      
      public override Task OnStarted() =>
          context.Cluster().Subscribe("topic", Context.ClusterIdentity()!);
      
      public override Task OnStopping() =>
          context.Cluster().Unsubscribe("topic", Context.ClusterIdentity()!);
      
      public override Task OnReceive()
      {
          if (Context.Message is ChatMessage msg)
              Console.WriteLine($"Received '{msg.Message}'");
          
          return Task.CompletedTask;
      }
  }
  ```

  - ```CodeGen```을 통해 ```<Actor>Base```를 만들었을 경우
  - 별도의 메소드를 제공하지 않고 ```OnReceive```에서 받아서 처리함

- ```csharp
  cluster.Unsubscribe("topic", pid);
  /* .. */
  cluster.Unsubscribe("topic", ClusterIdentity.Create("id", "kind"));
  ```

  - 구독 취소 시 가상 Actor인지 아닌지에 따라 다른 메소드를 사용

- BatchingProducer

  - Topic을 일괄 처리할 수 있게 해주는 producer

  - ```csharp
    await using var producer = Cluster.BatchingProducer("topic");
    var t1 = producer.ProduceAsync(new ChatMessage { Message = "Hello" });
    var t2 = prodcuer.ProduceAsync(new ChatMessage { Message = "world" });
    
    await Task.WhenAll(t1, t2);
    ```

  - ```csharp
    var tokenSource = new CancellationTokenSource();
    var t1 = producer.ProduceAsync(new ChatMessage { Message = "Hello" }, tokenSource.Token);
    tokenSource.Cancel();
    ```

  - ```csharp
    public delegate Task<PublishingErrorDecision> PublishingErrorHandler(
    	int retries, Exception e, PubSubBatch batch);
    ```

    - 위의 delegate를 override하여 에러 발생 시의 행동을 정할 수 있음
    - 필요한 리턴값(```PublishingErrorDecision```)
      - ```FailBatchAndStop``` - **default**, 모든 pending task를 실패처리하고 producer를 중지
      - ```FailBatchAndContinue``` - 현재 batch를 실패처리하고 다음 batch로 넘어감
      - ```RetryBatchAfter(TimeSpan delay)``` - ```delay``` 이후에 현재 batch를 재시도함
      - ```RetryBatchImmediately``` - 즉시 현재 batch를 재시도함



### 6. Gossip

- **Gossip(Epidemic) protocol** - 특정 그룹에게 메시지를 위한 방법 중 하나

  - ![Gossip1](D:\Projects\ProtoClusterSample\Images\Gossip1.jpg)
  - ![Gossip2](D:\Projects\ProtoClusterSample\Images\Gossip2.jpg)
  - 주기적으로 랜덤 타겟을 골라 Gossip message 전송
  - 그것을 받은 Infected node도 똑같이 행동

- Proto.Actor에서는 Gossip protocol을 사용해 내부적으로 클러스터 멤버를 관리

  - 한번에 전파할 Gossip message의 개수는 ```ClusterConfig.GossipFanout```의 수로 정해짐

- 직접 전파하려면 ```Cluster.Gossip.SetKey(key, value)```를 사용

  - key, value를 설정하면 이 정보를 클러스터의 다른 멤버들에게 전파함

- ```csharp
  var memberHeartbeats = await system.Cluster().Gossip.GetState<MemberHeartbeat>(GossipKeys.Heartbeat);
  
  var stats = (from x in memberHeartbeats
  	let memberId = x.Key
  	from y in x.Value.ActorStatistics.ActorCount
  	select (memberId, y.Key, y.Value))
      .ToList();
  ```

  - Gossip state Query

- ```csharp
  system.EventStream.Subscribe<GossipUpdate>(x => x.Key == GossipKeys.Heartbeat, update => {/*..*/});
  ```

  - Event Stream으로 Gossip state를 구독



### 7. Blocklist

- 클러스터의 각 멤버들은 문제가 생기면 block 될 수 있음

- 멤버가 block되었는지는 Gossip의 heartbeat가 timeout이 되었는지 여부로 판단

- 스스로 다운되었을 경우를 대비하는 방법

  - ```csharp
    builder.Services.AddHealthChecks().AddCheck<ActorSystemHealthCheck>("actor-system-health");
    /* .. */
    app.UseHealthChecks("/health");
    ```

    -  ASP.NET의 기능을 사용하여 Health Check에 사용할 미들웨어를 등록

  - ```csharp
    actorSystem.Shutdown.Register(() =>
    {
        actorSystem.Cluster().ShutdownAsync().Wait();
        
        Envrionment.Exit(1);
    });
    ```

    - 직접적으로 Actor System을 모니터링
