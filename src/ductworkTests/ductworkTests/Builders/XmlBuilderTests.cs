using System.Linq;
using ductwork;
using ductwork.Builders.Xml;
using ductworkTests.Components;
using ductworkTests.TestHelpers;
using NUnit.Framework;

#nullable enable
namespace ductworkTests.Builders;

public class XmlBuilderTests
{
    private static string GetGraphXml(string input)
    {
        return $"<?xml version=\"1.0\" encoding=\"utf-8\"?><graph>{input}</graph>";
    }

    [Test]
    public void CreatesGraphWithSimpleComponent()
    {
        const string expectedName = "Dummy";
        var expectedType = typeof(DummyComponent);

        var xml = GetGraphXml(@$"<component name=""{expectedName}"" type=""{expectedType.Name}""/>");
        var builder = XmlBuilder.LoadString(xml);

        Assert.IsEmpty(builder.Validate());

        var graph = builder.GetGraph();

        var component = graph.Components.FirstOrDefault();

        Assert.NotNull(component);
        Assert.AreEqual(expectedName, component?.DisplayName);
        Assert.AreEqual(expectedType, component?.GetType());
    }

    [Test]
    public void CreatesGraphWithComponentSettingString()
    {
        const string expectedString = "Testing";

        var xml = GetGraphXml(@$"
<component name=""Dummy"" type=""DummyComponent"">
    <set name=""DummyString"">{expectedString}</set>
</component>");
        var builder = XmlBuilder.LoadString(xml);

        Assert.IsEmpty(builder.Validate());

        var graph = builder.GetGraph();
        var component = graph.Components.OfType<DummyComponent>().First();

        Assert.AreEqual(expectedString, component.DummyString.Value);
    }

    [Test]
    public void CreatesGraphWithComponentSettingInt()
    {
        const int expectedInt = 1;

        var xml = GetGraphXml(@$"
<component name=""Dummy"" type=""DummyComponent"">
    <set name=""DummyInt"">{expectedInt}</set>
</component>");
        var builder = XmlBuilder.LoadString(xml);

        Assert.IsEmpty(builder.Validate());

        var graph = builder.GetGraph();
        var component = graph.Components.OfType<DummyComponent>().First();

        Assert.AreEqual(expectedInt, component.DummyInt.Value);
    }

    [Test]
    public void CreatesGraphWithComponentSettingArrayImplicit()
    {
        var expectedIntArray = new[] {2, 3};

        var xml = GetGraphXml(@$"
<component name=""Dummy"" type=""DummyComponent"">
    <set name=""DummyIntArray"">
        <item>{expectedIntArray[0]}</item>
        <item>{expectedIntArray[1]}</item>
    </set>
</component>");
        var builder = XmlBuilder.LoadString(xml);

        Assert.IsEmpty(builder.Validate());

        var graph = builder.GetGraph();
        var component = graph.Components.OfType<DummyComponent>().First();

        Assert.AreEqual(expectedIntArray.Length, component.DummyIntArray.Value.Length);
        Assert.AreEqual(expectedIntArray[0], component.DummyIntArray.Value[0]);
        Assert.AreEqual(expectedIntArray[1], component.DummyIntArray.Value[1]);
    }

    [Test]
    public void CreatesGraphWithComponentSettingArrayExplicit()
    {
        var expectedObjectArray = new object[] {"one", 2, 3.5};

        var xml = GetGraphXml(@$"
<component name=""Dummy"" type=""DummyComponent"">
    <set name=""DummyObjectArray"">
        <item>{expectedObjectArray[0]}</item>
        <item type=""int"">{expectedObjectArray[1]}</item>
        <item type=""float"">{expectedObjectArray[2]}</item>
    </set>
</component>");
        var builder = XmlBuilder.LoadString(xml);

        Assert.IsEmpty(builder.Validate());

        var graph = builder.GetGraph();
        var component = graph.Components.OfType<DummyComponent>().First();

        Assert.AreEqual(expectedObjectArray.Length, component.DummyObjectArray.Value.Length);
        Assert.AreEqual(expectedObjectArray[0], component.DummyObjectArray.Value[0]);
        Assert.AreEqual(expectedObjectArray[1], component.DummyObjectArray.Value[1]);
        Assert.AreEqual(expectedObjectArray[2], component.DummyObjectArray.Value[2]);
    }

    [Test]
    public void CreatesGraphWithConnections()
    {
        var xml = GetGraphXml($@"
<component name=""DummyA"" type=""DummyComponent""/>
<component name=""DummyB"" type=""DummyComponent""/>
<connection out=""DummyA.Out"" in=""DummyB.In""/>
");
        var builder = XmlBuilder.LoadString(xml);

        Assert.IsEmpty(builder.Validate());

        var graph = builder.GetGraph();
        var dummyA = graph.Components.OfType<DummyComponent>().First(c => c.DisplayName == "DummyA");
        var dummyB = graph.Components.OfType<DummyComponent>().First(c => c.DisplayName == "DummyB");

        var connection = graph.Connections.FirstOrDefault(c => c.Item1 == dummyA.Out && c.Item2 == dummyB.In);

        Assert.NotNull(connection);
    }

    [Test]
    public void CreatesGraphWithConnectionsImplicit()
    {
        var xml = GetGraphXml(@"
<component name=""DummyA"" type=""DummyComponent""/>
<component name=""DummyB"" type=""DummyComponent""/>
<connection out=""DummyA"" in=""DummyB""/>
");
        var builder = XmlBuilder.LoadString(xml);

        Assert.IsEmpty(builder.Validate());

        var graph = builder.GetGraph();

        var dummyA = graph.Components.OfType<DummyComponent>().First(c => c.DisplayName == "DummyA");
        var dummyB = graph.Components.OfType<DummyComponent>().First(c => c.DisplayName == "DummyB");
        var connection = graph.Connections.FirstOrDefault(c => c.Item1 == dummyA.Out && c.Item2 == dummyB.In);

        Assert.NotNull(connection);
    }

    [Test]
    public void ValidationFailsOnNonUniqueComponentNames()
    {
        const string expectedMessage = "Component name \"Dummy\" is not unique.";

        var xml = GetGraphXml(@"
<component name=""Dummy"" type=""DummyComponent""/>
<component name=""Dummy"" type=""DummyComponent""/>
");
        var builder = XmlBuilder.LoadString(xml);
        var exceptions = builder.Validate().ToArray();

        Assert.AreEqual(1, exceptions.Length);
        Assert.AreEqual(expectedMessage, exceptions[0].Message);
    }

    [Test]
    public void ValidationFailsOnMissingComponentNameAttribute()
    {
        const string expectedMessage = "Node requires \"name\" attribute.";

        var xml = GetGraphXml(@"<component type=""DummyComponent""/>");
        var builder = XmlBuilder.LoadString(xml);
        var exceptions = builder.Validate().ToArray();

        Assert.AreEqual(1, exceptions.Length);
        Assert.AreEqual(expectedMessage, exceptions[0].Message);
    }

    [Test]
    public void ValidationFailsOnMissingComponentTypeAttribute()
    {
        const string expectedMessage = "Node requires \"type\" attribute.";

        var xml = GetGraphXml(@"<component name=""Dummy""/>");
        var builder = XmlBuilder.LoadString(xml);
        var exceptions = builder.Validate().ToArray();

        Assert.AreEqual(1, exceptions.Length);
        Assert.AreEqual(expectedMessage, exceptions[0].Message);
    }

    [Test]
    public void ValidationFailsOnInvalidComponentType()
    {
        const string expectedMessage = "No loaded component type \"Invalid\".";

        var xml = GetGraphXml(@"<component name=""Dummy"" type=""Invalid""/>");
        var builder = XmlBuilder.LoadString(xml);
        var exceptions = builder.Validate().ToArray();

        Assert.AreEqual(1, exceptions.Length);
        Assert.AreEqual(expectedMessage, exceptions[0].Message);
    }

    [Test]
    public void ValidationFailsOnInvalidConnectionOutputComponent()
    {
        const string expectedMessage = "No component with name \"Invalid\".";

        var xml = GetGraphXml(@"
<component name=""Dummy"" type=""DummyComponent""/>
<connection out=""Invalid.Out"" in=""Dummy.In""/>
");
        var builder = XmlBuilder.LoadString(xml);
        var exceptions = builder.Validate().ToArray();

        Assert.AreEqual(1, exceptions.Length);
        Assert.AreEqual(expectedMessage, exceptions[0].Message);
    }

    [Test]
    public void ValidationFailsOnInvalidConnectionInputComponent()
    {
        const string expectedMessage = "No component with name \"Invalid\".";

        var xml = GetGraphXml(@"
<component name=""Dummy"" type=""DummyComponent""/>
<connection out=""Dummy.Out"" in=""Invalid.In""/>
");
        var builder = XmlBuilder.LoadString(xml);
        var exceptions = builder.Validate().ToArray();

        Assert.AreEqual(1, exceptions.Length);
        Assert.AreEqual(expectedMessage, exceptions[0].Message);
    }

    [Test]
    public void PathLoadsAndCreatesGraph_Receivers()
    {
        const string graphPath = "./Resources/TestGraphReceivers.xml";
        var builder = XmlBuilder.LoadPath(graphPath);

        Assert.IsEmpty(builder.Validate());

        var graph = builder.GetGraph();

        Assert.AreEqual(4, graph.Components.Count);
        Assert.AreEqual(2, graph.Components.OfType<SenderComponent>().Count());
        Assert.AreEqual(2, graph.Components.OfType<ReceiverComponent>().Count());

        var senderA = graph.Components.OfType<SenderComponent>().FirstOrDefault(c => c.DisplayName == "SenderA");
        var senderB = graph.Components.OfType<SenderComponent>().FirstOrDefault(c => c.DisplayName == "SenderB");
        var receiverA = graph.Components.OfType<ReceiverComponent>().FirstOrDefault(c => c.DisplayName == "ReceiverA");
        var receiverB = graph.Components.OfType<ReceiverComponent>().FirstOrDefault(c => c.DisplayName == "ReceiverB");

        Assert.NotNull(senderA);
        Assert.NotNull(senderB);
        Assert.NotNull(receiverA);
        Assert.NotNull(receiverB);

        Assert.AreEqual(3, graph.Connections.Count);

        (OutputPlug, InputPlug)? FindConnection(OutputPlug outputPlug, InputPlug inputPlug)
        {
            var items = graph.Connections
                .Where(item => item.Item1.Equals(outputPlug) && item.Item2.Equals(inputPlug))
                .ToArray();
            return items.Any() ? items.First() : null;
        }

        var senderAToReceiverA = FindConnection(senderA!.Out, receiverA!.In);
        var senderAToReceiverB = FindConnection(senderA!.Out, receiverB!.In);
        var senderBToReceiverB = FindConnection(senderB!.Out, receiverB!.In);
        
        Assert.NotNull(senderAToReceiverA);
        Assert.NotNull(senderAToReceiverB);
        Assert.NotNull(senderBToReceiverB);
    }
}