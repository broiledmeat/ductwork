using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using ductwork.FileLoaders;
using ductworkTests.Components;
using NUnit.Framework;

namespace ductworkTests
{
    public class GraphSerializationTests
    {
        [SetUp]
        public void Setup()
        {
        }
        
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
                    typeof(SenderComponent<string>),
                    typeof(SenderComponent<string>),
                    typeof(ReceiverComponent<string>),
                    typeof(ReceiverComponent<string>),
                }
                .SetEquals(components.Select(component => component.GetType()).ToHashSet()));
        
            graph.Execute().Wait();
        
            var receivers = components.OfType<ReceiverComponent<string>>().ToArray();
            var receiverA = receivers.FirstOrDefault(receiver => receiver.Values.Count == 3);
            var receiverB = receivers.FirstOrDefault(receiver => receiver.Values.Count == 5);
        
            Assert.NotNull(receiverA);
            Assert.NotNull(receiverB);
            Assert.That(new HashSet<string> {"foo", "bar", "baz"}.SetEquals(receiverA?.Values.ToHashSet()!));
            Assert.That(
                new HashSet<string> {"foo", "bar", "baz", "fizz", "buzz"}
                .SetEquals(receiverB?.Values.ToHashSet()!));
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
                    typeof(SenderComponent<int>),
                    typeof(SenderComponent<int>),
                    typeof(AdderComponent),
                    typeof(ReceiverComponent<int>),
                }
                .SetEquals(components.Select(component => component.GetType()).ToHashSet()));
        
            graph.Execute().Wait();
        
            var receiver = components.OfType<ReceiverComponent<int>>().First();
        
            Assert.That(new HashSet<int> {6, 8}.SetEquals(receiver.Values.ToHashSet()));
        }
        
        [Test]
        public void ThrowsOnComponentWithNoKey()
        {
            const string xml = @"
<graph>
    <component type=""SenderComponent""/>
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
    <component key=""SenderA"" type=""SenderComponent:int""/>
</graph>";
            Assert.Throws<InvalidOperationException>(() => GraphXmlLoader.LoadString(xmlMissingArgs));
            
            
            const string xmlIncorrectArgs = @"
<graph>
    <lib path="".\ductworkTests.dll""/>
    <component key=""SenderA"" type=""SenderComponent:string"">
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
    <component key=""SenderA"" type=""SenderComponent:int"">
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
    <component key=""SenderA"" type=""SenderComponent:int"">
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
    <component key=""Sender"" type=""SenderComponent:int""/>
    <component key=""Receiver"" type=""ReceiverComponent:int""/>
    <connection out=""Sender.Out"" in=""Receiver.Invalid""/>
</graph>";
            Assert.Throws<InvalidOperationException>(() => GraphXmlLoader.LoadString(xml));
        }
    }
}