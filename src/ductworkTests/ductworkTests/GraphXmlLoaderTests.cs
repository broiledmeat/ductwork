using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Schema;
using ductwork.Executors;
using ductwork.FileLoaders;
using ductworkTests.Components;
using NUnit.Framework;

#nullable enable
namespace ductworkTests;

public class GraphXmlLoaderTests
{
    [Test]
    public void ValidXmlLoadsAndExecutesWithExpectedOutput_Receivers()
    {
        const string graphPath = "./Resources/TestGraphReceivers.xml";
        var graph = GraphXmlLoader.LoadPath(graphPath);

        var components = graph.GetComponents().ToArray();
        var connections = graph.GetConnections().ToArray();

        Assert.AreEqual(4, components.Length);
        Assert.AreEqual(3, connections.Length);

        Assert.That(new HashSet<Type>
            {
                typeof(SenderComponent),
                typeof(SenderComponent),
                typeof(ReceiverComponent),
                typeof(ReceiverComponent),
            }
            .SetEquals(components.Select(component => component.GetType()).ToHashSet()));

        graph.GetExecutor<ThreadedExecutor>().Execute(CancellationToken.None).Wait();

        var receivers = components.OfType<ReceiverComponent>().ToArray();
        var receiverA = receivers.FirstOrDefault(receiver => receiver.Values.Count == 3);
        var receiverB = receivers.FirstOrDefault(receiver => receiver.Values.Count == 5);

        Assert.NotNull(receiverA);
        Assert.NotNull(receiverB);
        Assert.That(new HashSet<string> {"foo", "bar", "baz"}
            .SetEquals(receiverA!.Values.OfType<string>().ToHashSet()));
        Assert.That(new HashSet<string> {"foo", "bar", "baz", "fizz", "buzz"}
            .SetEquals(receiverB!.Values.OfType<string>().ToHashSet()));
    }

    [Test]
    public void ValidXmlLoadsAndExecutesWithExpectedOutput_Adder()
    {
        const string graphPath = "./Resources/TestGraphAdder.xml";
        var graph = GraphXmlLoader.LoadPath(graphPath);

        var components = graph.GetComponents().ToArray();
        var connections = graph.GetConnections().ToArray();

        Assert.AreEqual(4, components.Length);
        Assert.AreEqual(3, connections.Length);

        Assert.That(new HashSet<Type>
            {
                typeof(SenderComponent),
                typeof(SenderComponent),
                typeof(AdderComponent),
                typeof(ReceiverComponent),
            }
            .SetEquals(components.Select(component => component.GetType()).ToHashSet()));

        graph.GetExecutor<ThreadedExecutor>().Execute(CancellationToken.None).Wait();

        var receiver = components.OfType<ReceiverComponent>().FirstOrDefault();

        Assert.NotNull(receiver);
        Assert.That(new HashSet<int> {6, 8}
            .SetEquals(receiver!.Values.OfType<int>().ToHashSet()));
    }

    [Test]
    public void ThrowsOnComponentWithNoKey()
    {
        const string xml = @"
<graph>
    <component type=""ArrayGenericComponent""/>
</graph>";
        Assert.Throws<XmlSchemaException>(() => GraphXmlLoader.LoadString(xml));
    }

    [Test]
    public void ThrowsOnComponentWithNoType()
    {
        const string xml = @"
<graph>
    <component key=""test""/>
</graph>";
        Assert.Throws<XmlSchemaException>(() => GraphXmlLoader.LoadString(xml));
    }

    [Test]
    public void ThrowsOnComponentWithInvalidConstructor()
    {
        const string xmlMissingArgs = @"
<graph>
    <lib path="".\ductworkTests.dll""/>
    <component key=""Generic"" type=""ArrayGenericComponent:int""/>
</graph>";
        Assert.Throws<InvalidOperationException>(() => GraphXmlLoader.LoadString(xmlMissingArgs));


        const string xmlIncorrectArgs = @"
<graph>
    <lib path="".\ductworkTests.dll""/>
    <component key=""Generic"" type=""ArrayGenericComponent:string"">
        <arg type=""array:int""/>
    </component>
</graph>";
        Assert.Throws<InvalidOperationException>(() => GraphXmlLoader.LoadString(xmlIncorrectArgs));
    }

    [Test]
    public void ThrowsOnComponentWithNoArgType()
    {
        const string xml = @"
<graph>
    <lib path="".\ductworkTests.dll""/>
    <component key=""Generic"" type=""ArrayGenericComponent:int"">
        <arg/>
    </component>
</graph>";
        Assert.Throws<XmlSchemaException>(() => GraphXmlLoader.LoadString(xml));
    }

    [Test]
    public void ThrowsOnComponentWithInvalidArgType()
    {
        const string xml = @"
<graph>
    <lib path="".\ductworkTests.dll""/>
    <component key=""Generic"" type=""ArrayGenericComponent:int"">
        <arg type=""invalid""/>
    </component>
</graph>";
        Assert.Throws<XmlSchemaException>(() => GraphXmlLoader.LoadString(xml));
    }

    [Test]
    public void ThrowsOnConnectionWithInvalidOutputComponent()
    {
        const string xml = @"
<graph>
    <lib path="".\ductworkTests.dll""/>
    <component key=""Sender"" type=""SenderComponent:int""/>
    <component key=""Receiver"" type=""ReceiverComponent:int""/>
    <connection out=""invalid.Out"" in=""Receiver.In""/>
</graph>";
        Assert.Throws<InvalidOperationException>(() => GraphXmlLoader.LoadString(xml));
    }

    [Test]
    public void ThrowsOnConnectionWithInvalidInputComponent()
    {
        const string xml = @"
<graph>
    <lib path="".\ductworkTests.dll""/>
    <component key=""Sender"" type=""SenderComponent:int""/>
    <component key=""Receiver"" type=""ReceiverComponent:int""/>
    <connection out=""Sender.Out"" in=""Receiver.Invalid""/>
</graph>";
        Assert.Throws<InvalidOperationException>(() => GraphXmlLoader.LoadString(xml));
    }

    [Test]
    public void ThrowsOnConnectionWithInvalidOutputPlug()
    {
        const string xml = @"
<graph>
    <lib path="".\ductworkTests.dll""/>
    <component key=""Sender"" type=""SenderComponent:int""/>
    <component key=""Receiver"" type=""ReceiverComponent:int""/>
    <connection out=""Sender.Invalid"" in=""Receiver.In""/>
</graph>";
        Assert.Throws<InvalidOperationException>(() => GraphXmlLoader.LoadString(xml));
    }

    [Test]
    public void ThrowsOnConnectionWithInvalidInputPlug()
    {
        const string xml = @"
<graph>
    <lib path="".\ductworkTests.dll""/>
    <component key=""Sender"" type=""SenderComponent""/>
    <component key=""Receiver"" type=""ReceiverComponent""/>
    <connection out=""Sender.Out"" in=""Receiver.Invalid""/>
</graph>";
        Assert.Throws<InvalidOperationException>(() => GraphXmlLoader.LoadString(xml));
    }
}