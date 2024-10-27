using System;
using System.Linq;
using ductwork;
using ductwork.Builders;
using ductwork.Components;
using ductworkTests.TestHelpers;
using NLog;
using NUnit.Framework;

namespace ductworkTests.Builders;

public class GraphTests
{
    private const string Name = "test_graph";
    private static readonly Logger Logger = new NullLogger(new LogFactory());

    [Test]
    public void GetExecutorCreatesValidExecutor()
    {
        var graph = new Graph(Name, Logger, [], []);

        Assert.That(graph.Validate(), Is.Empty);

        graph.GetExecutor<DummyExecutor>();
    }

    [Test]
    public void GetExecutorAssertsOnInvalidExecutor()
    {
        var graph = new Graph(Name, Logger, [], []);

        Assert.That(graph.Validate(), Is.Empty);

        Assert.Catch<ArgumentException>(() => graph.GetExecutor<InvalidExecutor>());
    }

    [Test]
    public void GetExecutorPassesAlongArguments()
    {
        var dummyA = new DummyComponent();
        var dummyB = new DummyComponent();
        var components = new Component[] { dummyA, dummyB };
        var connections = new (OutputPlug, InputPlug)[] { (dummyA.Out, dummyB.In) };

        var graph = new Graph(Name, Logger, components, connections);

        Assert.That(graph.Validate(), Is.Empty);

        var executor = graph.GetExecutor<DummyExecutor>();

        Assert.That(components.ToHashSet().SetEquals(executor.Components));
        Assert.That(connections.ToHashSet().SetEquals(executor.Connections));
    }

    [Test]
    public void ValidationFailsOnConnectionOutputNotInComponents()
    {
        const string expectedMessage = "Connection output component was not in the graph.";

        var dummyA = new DummyComponent();
        var dummyB = new DummyComponent();
        var components = new Component[] { dummyB };
        var connections = new (OutputPlug, InputPlug)[] { (dummyA.Out, dummyB.In) };

        var graph = new Graph(Name, Logger, components, connections);
        var exceptions = graph.Validate().ToArray();

        Assert.That(1, Is.EqualTo(exceptions.Length));
        Assert.That(expectedMessage, Is.EqualTo(exceptions[0].Message));
    }

    [Test]
    public void ValidationFailsOnConnectionInputNotInComponents()
    {
        const string expectedMessage = "Connection input component was not in the graph.";

        var dummyA = new DummyComponent();
        var dummyB = new DummyComponent();
        var components = new Component[] { dummyA };
        var connections = new (OutputPlug, InputPlug)[] { (dummyA.Out, dummyB.In) };

        var graph = new Graph(Name, Logger, components, connections);
        var exceptions = graph.Validate().ToArray();

        Assert.That(1, Is.EqualTo(exceptions.Length));
        Assert.That(expectedMessage, Is.EqualTo(exceptions[0].Message));
    }
}