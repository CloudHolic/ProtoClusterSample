using Proto;
using Proto.Cluster;

namespace TestProviderSample.Grains;

public class CounterGrain : CounterGrainBase
{
    private int _value;

    public CounterGrain(IContext context) : base(context)
    {

    }

    public override Task OnStarted()
    {
        Console.WriteLine("Started Counter");
        return base.OnStarted();
    }

    public override Task OnStopping()
    {
        Console.WriteLine("Stopping Counter");
        return base.OnStopping();
    }

    public override Task OnStopped()
    {
        Console.WriteLine("Stopped Counter");
        return base.OnStopped();
    }

    public override Task Increment()
    {
        _value++;

        var identity = Context.ClusterIdentity()!;
        Console.WriteLine($"Incrementing {identity.Kind} / {identity.Identity}");

        return Task.CompletedTask;
    }

    public override Task OnReceive()
    {
        return Task.CompletedTask;
    }

    public override Task<CounterValue> GetCurrentValue()
    {
        return Task.FromResult(new CounterValue { Value = _value });
    }
}