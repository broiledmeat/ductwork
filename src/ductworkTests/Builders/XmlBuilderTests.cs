using System.Linq;
using ductwork;
using ductwork.Builders.Xml;
using ductworkTests.TestHelpers;
using NUnit.Framework;

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

        Assert.That(builder.Validate(), Is.Empty);

        var graph = builder.GetGraph();

        var component = graph.Components.FirstOrDefault();

        Assert.That(component, Is.Not.Null);
        Assert.That(expectedName, Is.EqualTo(component?.DisplayName));
        Assert.That(expectedType, Is.EqualTo(component?.GetType()));
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

        Assert.That(builder.Validate(), Is.Empty);

        var graph = builder.GetGraph();
        var component = graph.Components.OfType<DummyComponent>().First();

        Assert.That(expectedString, Is.EqualTo(component.DummyString.Value));
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

        Assert.That(builder.Validate(), Is.Empty);

        var graph = builder.GetGraph();
        var component = graph.Components.OfType<DummyComponent>().First();

        Assert.That(expectedInt, Is.EqualTo(component.DummyInt.Value));
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

        Assert.That(builder.Validate(), Is.Empty);

        var graph = builder.GetGraph();
        var component = graph.Components.OfType<DummyComponent>().First();

        Assert.That(expectedIntArray.Length, Is.EqualTo(component.DummyIntArray.Value.Length));
        Assert.That(expectedIntArray[0], Is.EqualTo(component.DummyIntArray.Value[0]));
        Assert.That(expectedIntArray[1], Is.EqualTo(component.DummyIntArray.Value[1]));
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

        Assert.That(builder.Validate(), Is.Empty);

        var graph = builder.GetGraph();
        var component = graph.Components.OfType<DummyComponent>().First();

        Assert.That(expectedObjectArray.Length, Is.EqualTo(component.DummyObjectArray.Value.Length));
        Assert.That(expectedObjectArray[0], Is.EqualTo(component.DummyObjectArray.Value[0]));
        Assert.That(expectedObjectArray[1], Is.EqualTo(component.DummyObjectArray.Value[1]));
        Assert.That(expectedObjectArray[2], Is.EqualTo(component.DummyObjectArray.Value[2]));
    }

    [Test]
    public void CreatesGraphWithConnections()
    {
        var xml = GetGraphXml(@"
<component name=""DummyA"" type=""DummyComponent""/>
<component name=""DummyB"" type=""DummyComponent""/>
<connection out=""DummyA.Out"" in=""DummyB.In""/>
");
        var builder = XmlBuilder.LoadString(xml);

        Assert.That(builder.Validate(), Is.Empty);

        var graph = builder.GetGraph();
        var dummyA = graph.Components.OfType<DummyComponent>().First(c => c.DisplayName == "DummyA");
        var dummyB = graph.Components.OfType<DummyComponent>().First(c => c.DisplayName == "DummyB");

        var connection = graph.Connections.FirstOrDefault(c => c.Item1 == dummyA.Out && c.Item2 == dummyB.In);

        Assert.That(connection, Is.Not.Null);
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

        Assert.That(builder.Validate(), Is.Empty);

        var graph = builder.GetGraph();

        var dummyA = graph.Components.OfType<DummyComponent>().First(c => c.DisplayName == "DummyA");
        var dummyB = graph.Components.OfType<DummyComponent>().First(c => c.DisplayName == "DummyB");
        var connection = graph.Connections.FirstOrDefault(c => c.Item1 == dummyA.Out && c.Item2 == dummyB.In);

        Assert.That(connection, Is.Not.Null);
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

        Assert.That(1, Is.EqualTo(exceptions.Length));
        Assert.That(expectedMessage, Is.EqualTo(exceptions[0].Message));
    }

    [Test]
    public void ValidationFailsOnMissingComponentNameAttribute()
    {
        const string expectedMessage = "Node requires \"name\" attribute.";

        var xml = GetGraphXml(@"<component type=""DummyComponent""/>");
        var builder = XmlBuilder.LoadString(xml);
        var exceptions = builder.Validate().ToArray();

        Assert.That(1, Is.EqualTo(exceptions.Length));
        Assert.That(expectedMessage, Is.EqualTo(exceptions[0].Message));
    }

    [Test]
    public void ValidationFailsOnMissingComponentTypeAttribute()
    {
        const string expectedMessage = "Node requires \"type\" attribute.";

        var xml = GetGraphXml(@"<component name=""Dummy""/>");
        var builder = XmlBuilder.LoadString(xml);
        var exceptions = builder.Validate().ToArray();

        Assert.That(1, Is.EqualTo(exceptions.Length));
        Assert.That(expectedMessage, Is.EqualTo(exceptions[0].Message));
    }

    [Test]
    public void ValidationFailsOnInvalidComponentType()
    {
        const string expectedMessage = "No loaded component type \"Invalid\".";

        var xml = GetGraphXml(@"<component name=""Dummy"" type=""Invalid""/>");
        var builder = XmlBuilder.LoadString(xml);
        var exceptions = builder.Validate().ToArray();

        Assert.That(1, Is.EqualTo(exceptions.Length));
        Assert.That(expectedMessage, Is.EqualTo(exceptions[0].Message));
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

        Assert.That(1, Is.EqualTo(exceptions.Length));
        Assert.That(expectedMessage, Is.EqualTo(exceptions[0].Message));
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

        Assert.That(1, Is.EqualTo(exceptions.Length));
        Assert.That(expectedMessage, Is.EqualTo(exceptions[0].Message));
    }

    [Test]
    public void PathLoadsAndCreatesGraph_Receivers()
    {
        const string graphPath = "./Resources/TestGraphReceivers.xml";
        var builder = XmlBuilder.LoadPath(graphPath);

        Assert.That(builder.Validate(), Is.Empty);

        var graph = builder.GetGraph();

        Assert.That(4, Is.EqualTo(graph.Components.Count));
        Assert.That(2, Is.EqualTo(graph.Components.OfType<SenderComponent>().Count()));
        Assert.That(2, Is.EqualTo(graph.Components.OfType<ReceiverComponent>().Count()));

        var senderA = graph.Components.OfType<SenderComponent>().FirstOrDefault(c => c.DisplayName == "SenderA");
        var senderB = graph.Components.OfType<SenderComponent>().FirstOrDefault(c => c.DisplayName == "SenderB");
        var receiverA = graph.Components.OfType<ReceiverComponent>().FirstOrDefault(c => c.DisplayName == "ReceiverA");
        var receiverB = graph.Components.OfType<ReceiverComponent>().FirstOrDefault(c => c.DisplayName == "ReceiverB");

        Assert.That(senderA, Is.Not.Null);
        Assert.That(senderB, Is.Not.Null);
        Assert.That(receiverA, Is.Not.Null);
        Assert.That(receiverB, Is.Not.Null);

        Assert.That(3, Is.EqualTo(graph.Connections.Count));

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

        Assert.That(senderAToReceiverA, Is.Not.Null);
        Assert.That(senderAToReceiverB, Is.Not.Null);
        Assert.That(senderBToReceiverB, Is.Not.Null);
    }
}