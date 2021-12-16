using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using ductwork.Artifacts;
using ductwork.Components;
using NLog;
using NLog.Targets;

#nullable enable
namespace ductwork.FileLoaders;

public static class GraphXmlLoader
{
    private const string ArrayValueTypeName = "array";

    private record ValueConverter(Type Type, Func<XmlNode, object> Convert);

    private static readonly Dictionary<string, ValueConverter> ValueConverters = new()
    {
        {"string", new ValueConverter(typeof(string), node => node.InnerText.Trim())},
        {"int", new ValueConverter(typeof(int), node => Convert.ToInt32(node.InnerText.Trim()))},
        {"float", new ValueConverter(typeof(float), node => Convert.ToSingle(node.InnerText.Trim()))},
        {"bool", new ValueConverter(typeof(bool), node => Convert.ToBoolean(node.InnerText.Trim()))}
    };

    public static Graph LoadPath(string xmlFilepath)
    {
        var document = new XmlDocument();
        document.Load(Path.GetFullPath(xmlFilepath));
        return LoadInternal(document);
    }

    public static Graph LoadString(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        return LoadInternal(document);
    }

    private static Graph LoadInternal(XmlDocument document)
    {
        var graph = new Graph();

        document
            .SelectXPath("/graph/config")
            .ForEach(node => ProcessConfigNode(node, graph));

        var assemblies = document
            .SelectXPath("/graph/lib")
            .Select(ProcessLibNode)
            .Select(Assembly.LoadFrom)
            .Concat(new[] {Assembly.GetExecutingAssembly()})
            .ToArray();

        var componentTypes = assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsAssignableTo(typeof(Component)))
            .ToDictionary(
                type => type.IsGenericType ? type.Name[..type.Name.IndexOf('`')] : type.Name,
                type => type);

        var artifactTypes = assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsAssignableTo(typeof(IArtifact)))
            .ToDictionary(type => type.Name, type => type);

        var components = document
            .SelectXPath("/graph/component")
            .Select(node => ProcessComponentNode(node, componentTypes, artifactTypes))
            .ToDictionary(pair => pair.Item1, pair => pair.Item2);

        components.Values
            .ForEach(component => graph.Add(component));

        document
            .SelectXPath("/graph/connection")
            .Select(node => ProcessConnectionNode(node, components))
            .ForEach(pair => graph.Connect(pair.Item1, pair.Item2));

        return graph;
    }

    private static void ProcessConfigNode(XmlNode node, Graph graph)
    {
        foreach (var logNode in node.SelectXPath("logfile"))
        {
            var fullPath = Path.GetFullPath(RequireAttribute(logNode, "path"));
            graph.Log.Debug($"Added file log \"{fullPath}\"");
            var config = LogManager.Configuration;
            config.AddRule(
                LogLevel.Trace,
                LogLevel.Fatal,
                new FileTarget
                {
                    FileName = fullPath,
                    Layout = Graph.DefaultLogFormat
                },
                graph.Log.Name);
            LogManager.Configuration = config;
        }
    }

    private static string ProcessLibNode(XmlNode node)
    {
        return Path.GetFullPath(RequireAttribute(node, "path"));
    }

    private static (string, Component) ProcessComponentNode(
        XmlNode node,
        IReadOnlyDictionary<string, Type> componentTypes,
        IReadOnlyDictionary<string, Type> artifactTypes)
    {
        var key = RequireAttribute(node, "key");
        var fullTypeName = RequireAttribute(node, "type");

        var (mainTypeName, subTypeName) = SplitTypeNames(fullTypeName);

        var componentType = componentTypes.GetValueOrDefault(mainTypeName);
        if (componentType == null)
        {
            throw new InvalidOperationException($"Could not find Component type named {mainTypeName}.");
        }

        var componentArgs = node
            .SelectXPath("arg")
            .Select(ProcessArgNode)
            .ToArray();
        var componentArgTypes = componentArgs.Select(arg => arg.GetType()).ToArray();

        if (componentType.IsGenericType)
        {
            var subType = artifactTypes.GetValueOrDefault(subTypeName ?? "") ?? GetValueConverter(subTypeName).Type;
            componentType = componentType.MakeGenericType(subType);
        }

        var componentConstructor = componentType.GetConstructor(componentArgTypes);

        if (componentConstructor == null)
        {
            var argNames = string.Join(", ", componentArgTypes.Select(type => type.Name));
            throw new InvalidOperationException($"Could not find Component constructor for {argNames}");
        }

        var component = (Component) componentConstructor.Invoke(componentArgs);
        component.DisplayName = key;
        return (key, component);
    }

    private static (OutputPlug, InputPlug) ProcessConnectionNode(
        XmlNode node,
        IReadOnlyDictionary<string, Component> components)
    {
        var outName = RequireAttribute(node, "out");
        var inName = RequireAttribute(node, "in");

        var (outComponentName, outPlugName) = SplitConnectionNames(outName);
        var (inComponentName, inPlugName) = SplitConnectionNames(inName);

        var outComponent = components.GetValueOrDefault(outComponentName)
            ?? throw new InvalidOperationException($"No component with key \"{outComponentName}\".");
        var inComponent = components.GetValueOrDefault(inComponentName)
            ?? throw new InvalidOperationException($"No component with key \"{inComponentName}\".");

        var outField = outComponent.GetType().GetField(outPlugName)
            ?? throw new InvalidOperationException($"Component \"{outComponentName}\" does not have plug \"{outPlugName}\".");
        var inField = inComponent.GetType().GetField(inPlugName)
            ?? throw new InvalidOperationException($"Component \"{inComponentName}\" does not have plug \"{inPlugName}\".");

        var output = (OutputPlug) outField.GetValue(outComponent)!;
        var input = (InputPlug) inField.GetValue(inComponent)!;

        return (output, input);
    }

    private static object ProcessArgNode(XmlNode node)
    {
        var typeName = RequireAttribute(node, "type");

        var (mainTypeName, subTypeName) = SplitTypeNames(typeName);

        if (mainTypeName != ArrayValueTypeName)
        {
            return GetValueConverter(mainTypeName).Convert(node);
        }

        var converter = GetValueConverter(subTypeName);
        var childValues = node
            .SelectXPath("item")
            .Select(child => converter.Convert(child))
            .ToArray();
        var value = Array.CreateInstance(converter.Type, childValues.Length);

        Array.Copy(childValues, value, childValues.Length);

        return value;
    }

    private static ValueConverter GetValueConverter(string? name)
    {
        if (name == null || !ValueConverters.ContainsKey(name))
        {
            throw new XmlSchemaException($"Unsupported type \"{name}\".");
        }

        return ValueConverters[name];
    }

    private static (string, string?) SplitTypeNames(string name)
    {
        var split = name.IndexOf(':');
        return split == -1 ? (name, null) : (name[..split], name[(split + 1)..]);
    }

    private static (string, string) SplitConnectionNames(string name)
    {
        var parts = name.Split('.');

        if (parts.Length != 2)
        {
            throw new InvalidOperationException("Connection name must match format \"Component.Plug\"");
        }

        return (parts[0], parts[1]);
    }

    private static string RequireAttribute(XmlNode node, string name)
    {
        return node.Attributes?[name]?.Value ??
               throw new XmlSchemaException($"Node must have a `{name}` attribute.");
    }

    private static string? GetAttribute(XmlNode node, string name)
    {
        return node.Attributes?[name]?.Value;
    }
}