using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using ductwork.Artifacts;
using ductwork.Components;
using NLog;
using NLog.Targets;
using Component = ductwork.Components.Component;

#nullable enable
namespace ductwork.FileLoaders;

public static class GraphXmlLoader
{
    private const string ArrayValueTypeName = "array";

    private record ValueConverter(Type Type, Func<XmlNode, object> Convert);

    private static readonly HashSet<(Type, string)> ValueTypeNames = new()
    {
        (typeof(string), "string"),
        (typeof(int), "int"),
        (typeof(float), "float"),
        (typeof(bool), "bool"),
    };

    private static readonly Dictionary<Type[], ValueConverter> ValueConverters = new()
    {
        {new[] {typeof(object)}, new ValueConverter(typeof(string), node => node.InnerText.Trim())},
        {new[] {typeof(string)}, new ValueConverter(typeof(string), node => node.InnerText.Trim())},
        {new[] {typeof(int)}, new ValueConverter(typeof(int), node => Convert.ToInt32(node.InnerText.Trim()))},
        {new[] {typeof(float)}, new ValueConverter(typeof(float), node => Convert.ToSingle(node.InnerText.Trim()))},
        {new[] {typeof(bool)}, new ValueConverter(typeof(bool), node => Convert.ToBoolean(node.InnerText.Trim()))}
    };

    public static GraphBuilder LoadPath(string xmlFilepath)
    {
        var document = new XmlDocument();
        document.Load(Path.GetFullPath(xmlFilepath));
        return LoadInternal(document);
    }

    public static GraphBuilder LoadString(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        return LoadInternal(document);
    }

    private static GraphBuilder LoadInternal(XmlDocument document)
    {
        var graph = new GraphBuilder();

        document
            .SelectXPath("/graph/config")
            .ForEach(node => ProcessConfigNode(node, graph));

        var assemblies = document
            .SelectXPath("/graph/lib")
            .Select(ProcessLibNode)
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

    private static void ProcessConfigNode(XmlNode node, GraphBuilder graph)
    {
        var displayName = GetAttribute(node, "displayname");
        
        if (displayName != null)
        {
            graph.DisplayName = displayName;
        }

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
                    Layout = GraphBuilder.DefaultLogFormat
                },
                graph.Log.Name);
            LogManager.Configuration = config;
        }
    }

    private static Assembly ProcessLibNode(XmlNode node)
    {
        var path = Path.GetFullPath(RequireAttribute(node, "path"));
        return Assembly.LoadFrom(path);
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

        if (componentType.IsGenericType)
        {
            var subType = artifactTypes.GetValueOrDefault(subTypeName ?? "") ?? GetValueConverter(subTypeName).Type;
            componentType = componentType.MakeGenericType(subType);
        }

        var componentConstructor = componentType.GetConstructor(Array.Empty<Type>());

        if (componentConstructor == null)
        {
            throw new InvalidOperationException($"Could not find empty Component constructor for {mainTypeName}.");
        }

        var component = (Component) componentConstructor.Invoke(Array.Empty<object>());
        component.DisplayName = key;

        // Set component Setting<T> fields.
        node
            .SelectXPath("set")
            .ForEach(settingNode => ProcessSettingNode(settingNode, component));

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
                       ?? throw new InvalidOperationException(
                           $"Component \"{outComponentName}\" does not have plug \"{outPlugName}\".");
        var inField = inComponent.GetType().GetField(inPlugName)
                      ?? throw new InvalidOperationException(
                          $"Component \"{inComponentName}\" does not have plug \"{inPlugName}\".");

        var output = (OutputPlug) outField.GetValue(outComponent)!;
        var input = (InputPlug) inField.GetValue(inComponent)!;

        return (output, input);
    }

    private static void ProcessSettingNode(XmlNode node, Component component)
    {
        var settingName = RequireAttribute(node, "name");

        var settingFieldInfo = component.GetType().GetField(settingName)
                               ?? throw new InvalidOperationException($"No field on component \"{settingName}\"");

        if (!settingFieldInfo.FieldType.IsAssignableTo(typeof(ISetting)))
        {
            throw new InvalidOperationException($"Component field \"{settingName}\" is not an {nameof(ISetting)}");
        }

        if (settingFieldInfo.GetValue(component) is not ISetting settingField)
        {
            throw new InvalidOperationException($"Could not get Component field \"{settingName}\"");
        }

        object settingValue;

        if (!settingField.Type.IsArray)
        {
            var converter = GetValueConverter(settingField.Type);
            settingValue = converter.Convert(node);
        }
        else
        {
            var arrayType = settingField.Type.GetElementType() ?? throw new InvalidOperationException();
            var converter = GetValueConverter(arrayType);
            var childValues = node
                .SelectXPath("item")
                .Select(child =>
                {
                    var childConverter = converter;
                    var childType = GetAttribute(child, "type");
                    if (childType != null)
                    {
                        childConverter = GetValueConverter(childType);
                    }

                    return childConverter.Convert(child);
                })
                .ToArray();
            var arrayValue = Array.CreateInstance(arrayType, childValues.Length);
            Array.Copy(childValues, arrayValue, childValues.Length);
            settingValue = arrayValue;
        }

        var settingType = typeof(Setting<>).MakeGenericType(settingField.Type);

        if (Activator.CreateInstance(settingType, settingValue) is not ISetting setting)
        {
            throw new InvalidOperationException($"Unable to instantiate setting for \"{settingName}\"");
        }

        settingFieldInfo.SetValue(component, setting);
    }

    private static ValueConverter GetValueConverter(Type[] types)
    {
        var converter = ValueConverters
            .Where(pair => pair.Key.ToHashSet().SetEquals(types))
            .Select(pair => pair.Value)
            .FirstOrDefault();

        if (converter == null)
        {
            throw new XmlSchemaException($"Unsupported type \"{types}\".");
        }

        return converter;
    }

    private static ValueConverter GetValueConverter(Type type)
    {
        return GetValueConverter(new[] {type});
    }

    private static ValueConverter GetValueConverter(string? name)
    {
        var type = ValueTypeNames.Where(pair => pair.Item2 == name).Select(pair => pair.Item1).FirstOrDefault();

        if (type == null)
        {
            throw new XmlSchemaException($"Unsupported type \"{name}\".");
        }

        return GetValueConverter(type);
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