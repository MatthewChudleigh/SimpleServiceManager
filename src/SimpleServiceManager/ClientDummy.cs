using System.Diagnostics.CodeAnalysis;

namespace SimpleServiceManager;

public class ClientDummyManager(int totalLoops) : IClientManager
{
    public bool TryInitialiseClient([NotNullWhen(true)] out IClient? client)
    {
        client = new ClientDummy(totalLoops);
        return true;
    }
}

public class ClientDummy(int totalLoops) : IClient
{
    private int _loops;
    
    public void Start()
    {
        _loops = totalLoops;
    }

    public bool IsRunning()
    {
        _loops--;
        return _loops > 0;
    }

    public bool Stop()
    {
        return true;
    }
}