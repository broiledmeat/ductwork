using System;
using System.Linq;
using ductwork;
using ductwork.Components;
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

        var graph = new Graph {DisplayName = nameof(ExecutesWithExpectedOutput_SingleOutToMultiIn)};
        var sender = new SenderComponent(values) {DisplayName = "Sender"};
        var receiverA = new ReceiverComponent {DisplayName = "ReceiverA"};
        var receiverB = new ReceiverComponent {DisplayName = "ReceiverB"};

        graph.Add(sender, receiverA, receiverB);
        graph.Connect(sender.Out, receiverA.In);
        graph.Connect(sender.Out, receiverB.In);

        graph.Execute().Wait();

        Assert.IsTrue(values.All(receiverA.Values.Contains));
        Assert.IsTrue(values.All(receiverB.Values.Contains));
    }

    [Test]
    public void ExecutesWithExpectedOutput_MultiOutToSingleIn()
    {
        var valuesA = new object[] {1, "two", 3, 4};
        var valuesB = new object[] {5, 6, 7, 8, 9, "ten"};
        var valuesC = new object[] {11, 12};

        var graph = new Graph {DisplayName = nameof(ExecutesWithExpectedOutput_MultiOutToSingleIn)};
        var senderA = new SenderComponent(valuesA) {DisplayName = "SenderA"};
        var senderB = new SenderComponent(valuesB) {DisplayName = "SenderB"};
        var senderC = new SenderComponent(valuesC) {DisplayName = "SenderC"};
        var receiver = new ReceiverComponent {DisplayName = "Receiver"};

        graph.Add(senderA, senderB, senderC, receiver);
        graph.Connect(senderA.Out, receiver.In);
        graph.Connect(senderB.Out, receiver.In);
        graph.Connect(senderC.Out, receiver.In);

        graph.Execute().Wait();

        Assert.IsTrue(valuesA.All(receiver.Values.Contains));
        Assert.IsTrue(valuesB.All(receiver.Values.Contains));
    }

    [Test]
    public void ExecutesWithExpectedOutput_Adder()
    {
        const int valueA = 1;
        const int valueB = 2;

        var graph = new Graph {DisplayName = nameof(ExecutesWithExpectedOutput_Adder)};
        var senderA = new SenderComponent(new object[] {valueA}) {DisplayName = "SenderA"};
        var senderB = new SenderComponent(new object[] {valueB}) {DisplayName = "SenderB"};
        var adder = new AdderComponent {DisplayName = "Adder"};
        var receiver = new ReceiverComponent {DisplayName = "Receiver"};

        graph.Add(senderA, senderB, adder, receiver);
        graph.Connect(senderA.Out, adder.InX);
        graph.Connect(senderB.Out, adder.InY);
        graph.Connect(adder.Out, receiver.In);

        graph.Execute().Wait();

        Assert.AreEqual(valueA + valueB, receiver.Values.FirstOrDefault());
    }
}