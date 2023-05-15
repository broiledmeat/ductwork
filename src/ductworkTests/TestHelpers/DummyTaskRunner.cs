using System;
using System.Threading;
using System.Threading.Tasks;
using ductwork.TaskRunners;

namespace ductworkTests.TestHelpers;

internal class DummyTaskRunner : TaskRunner
{
    public override Task RunAsync(Func<Task> func, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public override Task WaitAsync(CancellationToken token)
    {
        throw new NotImplementedException();
    }
}