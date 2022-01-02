using System;
using System.Linq;
using System.Threading;
using ductwork;
using ductwork.Executors;
using ductworkTests.Components;
using NUnit.Framework;

#nullable enable
namespace ductworkTests;

public class GraphTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ExecutesWithExpectedOutput_SingleOutToMultiIn()
    {
        var values = new object[] {1, "two"};

        var graph = new GraphBuilder(nameof(ExecutesWithExpectedOutput_SingleOutToMultiIn));
        var sender = new SenderComponent {DisplayName = "Sender", Values = values};
        var receiverA = new ReceiverComponent {DisplayName = "ReceiverA"};
        var receiverB = new ReceiverComponent {DisplayName = "ReceiverB"};

        graph.Add(sender, receiverA, receiverB);
        graph.Connect(sender.Out, receiverA.In);
        graph.Connect(sender.Out, receiverB.In);

        graph.GetExecutor<ThreadedExecutor>().Execute(CancellationToken.None).Wait();

        Assert.IsTrue(values.All(receiverA.Values.Contains));
        Assert.IsTrue(values.All(receiverB.Values.Contains));
    }

    [Test]
    public void ExecutesWithExpectedOutput_MultiOutToSingleIn()
    {
        var valuesA = new object[] {1, "two", 3, 4};
        var valuesB = new object[] {5, 6, 7, 8, 9, "ten"};
        var valuesC = new object[] {11, 12};

        var graph = new GraphBuilder(nameof(ExecutesWithExpectedOutput_MultiOutToSingleIn));
        var senderA = new SenderComponent {DisplayName = "SenderA", Values = valuesA};
        var senderB = new SenderComponent {DisplayName = "SenderB", Values = valuesB};
        var senderC = new SenderComponent {DisplayName = "SenderC", Values = valuesC};
        var receiver = new ReceiverComponent {DisplayName = "Receiver"};

        graph.Add(senderA, senderB, senderC, receiver);
        graph.Connect(senderA.Out, receiver.In);
        graph.Connect(senderB.Out, receiver.In);
        graph.Connect(senderC.Out, receiver.In);

        graph.GetExecutor<ThreadedExecutor>().Execute(CancellationToken.None).Wait();

        Assert.IsTrue(valuesA.All(receiver.Values.Contains));
        Assert.IsTrue(valuesB.All(receiver.Values.Contains));
    }

    [Test]
    public void ExecutesWithExpectedOutput_Adder()
    {
        const int valueA = 1;
        const int valueB = 2;

        var graph = new GraphBuilder(nameof(ExecutesWithExpectedOutput_Adder));
        var senderA = new SenderComponent {DisplayName = "SenderA", Values = new object[] {valueA}};
        var senderB = new SenderComponent {DisplayName = "SenderB", Values = new object[] {valueB}};
        var adder = new AdderComponent {DisplayName = "Adder"};
        var receiver = new ReceiverComponent {DisplayName = "Receiver"};

        graph.Add(senderA, senderB, adder, receiver);
        graph.Connect(senderA.Out, adder.InX);
        graph.Connect(senderB.Out, adder.InY);
        graph.Connect(adder.Out, receiver.In);

        graph.GetExecutor<ThreadedExecutor>().Execute(CancellationToken.None).Wait();

        Assert.AreEqual(valueA + valueB, receiver.Values.FirstOrDefault());
    }

    [Test]
    public void ThrowsOnConnectionWithUnaddedComponents()
    {
        var graph = new GraphBuilder(nameof(ThrowsOnConnectionWithUnaddedComponents));
        var dirIter = new SenderComponent {DisplayName = "Sender"};
        var receiver = new ReceiverComponent {DisplayName = "Receiver"};

        Assert.Throws<InvalidOperationException>(() => graph.Connect(dirIter.Out, receiver.In));
    }
}