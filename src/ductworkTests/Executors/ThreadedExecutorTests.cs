using System.Linq;
using System.Threading;
using ductwork;
using ductwork.Builders;
using ductwork.Components;
using ductwork.Executors;
using ductworkTests.TestHelpers;
using NLog;
using NUnit.Framework;

namespace ductworkTests.Executors;

public class ThreadedExecutorTests
{
    private const string Name = "test_graph";
    private static readonly Logger Logger = new NullLogger(new LogFactory());

    [Test]
    public void ExecutesWithExpectedOutput_SingleOutToMultiIn()
    {
        var expectedValues = new object[] {1, "two"};

        var sender = new SenderComponent {Values = expectedValues};
        var receiverA = new ReceiverComponent();
        var receiverB = new ReceiverComponent();
        var components = new Component[] {sender, receiverA, receiverB};
        var connections = new (OutputPlug, InputPlug)[] {(sender.Out, receiverA.In), (sender.Out, receiverB.In)};
        var graph = new Graph(Name, Logger, components, connections);

        Assert.That(graph.Validate(), Is.Empty);

        graph.GetExecutor<ThreadedExecutor>().Execute(CancellationToken.None).Wait();

        Assert.That(expectedValues.All(receiverA.Values.Contains));
        Assert.That(expectedValues.All(receiverB.Values.Contains));
    }

    [Test]
    public void ExecutesWithExpectedOutput_MultiOutToSingleIn()
    {
        var expectedValuesA = new object[] {1, "two", 3, 4};
        var expectedValuesB = new object[] {5, 6, 7, 8, 9, "ten"};
        var expectedValuesC = new object[] {11, 12};

        var senderA = new SenderComponent {Values = expectedValuesA};
        var senderB = new SenderComponent {Values = expectedValuesB};
        var senderC = new SenderComponent {Values = expectedValuesC};
        var receiver = new ReceiverComponent();
        var components = new Component[] {senderA, senderB, senderC, receiver};
        var connections = new (OutputPlug, InputPlug)[]
        {
            (senderA.Out, receiver.In),
            (senderB.Out, receiver.In),
            (senderC.Out, receiver.In),
        };
        var graph = new Graph(Name, Logger, components, connections);

        Assert.That(graph.Validate(), Is.Empty);

        graph.GetExecutor<ThreadedExecutor>().Execute(CancellationToken.None).Wait();

        Assert.That(expectedValuesA.All(receiver.Values.Contains));
        Assert.That(expectedValuesB.All(receiver.Values.Contains));
        Assert.That(expectedValuesC.All(receiver.Values.Contains));
    }

    [Test]
    public void ExecutesWithExpectedOutput_Adder()
    {
        const int expectedValueA = 1;
        const int expectedValueB = 2;

        var senderA = new SenderComponent {Values = new object[] {expectedValueA}};
        var senderB = new SenderComponent {Values = new object[] {expectedValueB}};
        var adder = new AdderComponent();
        var receiver = new ReceiverComponent();
        var components = new Component[] {senderA, senderB, adder, receiver};
        var connections = new (OutputPlug, InputPlug)[]
        {
            (senderA.Out, adder.InX),
            (senderB.Out, adder.InY),
            (adder.Out, receiver.In),
        };
        var graph = new Graph(Name, Logger, components, connections);

        Assert.That(graph.Validate(), Is.Empty);

        graph.GetExecutor<ThreadedExecutor>().Execute(CancellationToken.None).Wait();

        Assert.That(expectedValueA + expectedValueB, Is.EqualTo(receiver.Values.FirstOrDefault()));
    }

    [Test]
    [CancelAfter(2000)]
    public void ExecutesWithOrphanInputWithoutHanging()
    {
        var receiver = new ReceiverComponent();
        var components = new Component[] {receiver};
        var connections = new (OutputPlug, InputPlug)[] { };
        var graph = new Graph(Name, Logger, components, connections);

        Assert.That(graph.Validate(), Is.Empty);

        graph.GetExecutor<ThreadedExecutor>().Execute(CancellationToken.None).Wait();
    }
}