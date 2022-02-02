using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Schema;
using ductwork.Artifacts;
using ductwork.Components;
using NLog;
using NLog.Targets;

#nullable enable
namespace ductwork.Builders.Xml;

public class XmlBuilder : IBuilder
{
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

    private bool _hasInstalledLoadingHook = false;
    private string[] _libraryLookupPaths = Array.Empty<string>();
    private XmlDocument _document = new();
    private Logger? _logger;
    private string _defaultDisplayName = "graph";

    private XmlBuilder()
    {
    }

    public Logger Logger => _logger ??= Logging.GetLogger($"{Guid.NewGuid().ToString()}_{DisplayName}");

    public string DisplayName => HasAttribute(_document, "name")
        ? GetAttribute(_document, "name")
        : _defaultDisplayName;

    private IEnumerable<LibraryDef> LibraryDefs => _document
        .SelectXPath("/graph/lib")
        .Select(node => new LibraryDef(node));

    private IEnumerable<FileLogDef> LoggerDefs => _document
        .SelectXPath("/graph/logfile")
        .Select(node => new FileLogDef(node));

    private IEnumerable<ComponentDef> ComponentDefs => _document
        .SelectXPath("/graph/component")
        .Select(node => new ComponentDef(node));

    private IEnumerable<ConnectionDef> ConnectionDefs => _document
        .SelectXPath("/graph/connection")
        .Select(node => new ConnectionDef(node));

    public IEnumerable<Exception> Validate()
    {
        var nodes = Enumerable.Empty<object>()
            .Concat(LibraryDefs)
            .Concat(LoggerDefs)
            .Concat(ComponentDefs)
            .Concat(ConnectionDefs)
            .OfType<NodeBackedDef>()
            .ToList();

        // Validate individual nodes and bubble up their exceptions. Remove any excepted nodes from the nodes list.
        foreach (var node in nodes.ToArray())
        {
            var exceptions = node.Validate().ToArray();

            if (!exceptions.Any())
            {
                continue;
            }

            foreach (var exception in exceptions)
            {
                yield return exception;
            }

            nodes.Remove(node);
        }

        var assemblies = GetLibraryAssemblies();
        var componentTypes = GetAssembliesComponentTypes(assemblies);

        // Validate components
        var components = nodes.OfType<ComponentDef>().ToArray();
        var componentNames = components.Select(def => def.Name).ToArray();
        var exceptedComponentNames = new HashSet<string>();

        foreach (var def in components)
        {
            if (!exceptedComponentNames.Contains(def.Name) && componentNames.Count(name => name == def.Name) > 1)
            {
                yield return new InvalidOperationException($"Component name \"{def.Name}\" is not unique.");
                exceptedComponentNames.Add(def.Name);
            }

            if (!componentTypes.ContainsKey(def.TypeName))
            {
                yield return new InvalidOperationException($"No loaded component type \"{def.TypeName}\".");
            }
        }

        // Validate connections
        var connections = nodes.OfType<ConnectionDef>().ToArray();

        foreach (var def in connections)
        {
            if (!componentNames.Contains(def.OutputComponentName))
            {
                yield return new InvalidOperationException($"No component with name \"{def.OutputComponentName}\".");
            }

            if (!componentNames.Contains(def.InputComponentName))
            {
                yield return new InvalidOperationException($"No component with name \"{def.InputComponentName}\".");
            }
        }

        // TODO: Value converter validation
    }

    public Graph GetGraph()
    {
        if (LoggerDefs.Any())
        {
            foreach (var logDef in LoggerDefs)
            {
                Logging.AddRule(
                    Logger.Name,
                    new FileTarget
                    {
                        FileName = logDef.FilePath,
                        Layout = Logging.DefaultLogFormat
                    },
                    logDef.MinLevel,
                    logDef.MaxLevel);
            }
        }

        var assemblies = GetLibraryAssemblies();
        var componentTypes = GetAssembliesComponentTypes(assemblies);
        var artifactTypes = GetAssembliesArtifactTypes(assemblies);

        var components = ComponentDefs
            .ToDictionary(
                def => def.Name,
                def => CreateComponent(def, componentTypes, artifactTypes));

        var outputPlugs = components
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value
                    .GetFields<OutputPlug>()
                    .ToDictionary(result => result.Info.Name, result => result.Value));
        var inputPlugs = components
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value
                    .GetFields<InputPlug>()
                    .ToDictionary(result => result.Info.Name, result => result.Value));

        var connections = ConnectionDefs
            .Select(def => (
                outputPlugs[def.OutputComponentName][def.OutputPlugName],
                inputPlugs[def.InputComponentName][def.InputPlugName]))
            .ToHashSet();

        return new Graph(DisplayName, Logger, components.Values, connections);
    }

    public static XmlBuilder LoadPath(string xmlFilepath)
    {
        var document = new XmlDocument();
        document.Load(Path.GetFullPath(xmlFilepath));
        return new XmlBuilder
        {
            _document = document,
            _libraryLookupPaths = new[] {Path.GetDirectoryName(xmlFilepath)!}
        };
    }

    public static XmlBuilder LoadString(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        return new XmlBuilder {_document = document};
    }

    private static Component CreateComponent(
        ComponentDef componentDef,
        IReadOnlyDictionary<string, Type> componentTypes,
        IReadOnlyDictionary<string, Type> artifactTypes)
    {
        var (mainTypeName, subTypeName) = SplitTypeNames(componentDef.TypeName);
        var componentType = componentTypes[mainTypeName];

        if (componentType.IsGenericType)
        {
            var subType = artifactTypes.GetValueOrDefault(subTypeName ?? "") ?? GetValueConverter(subTypeName).Type;
            componentType = componentType.MakeGenericType(subType);
        }

        var componentConstructor = componentType.GetConstructor(Array.Empty<Type>())!;
        var component = (Component) componentConstructor.Invoke(Array.Empty<object>());
        component.DisplayName = componentDef.Name;

        componentDef.Settings.ForEach(settingDef => ProcessSettingDef(component, settingDef));

        return component;
    }

    private static void ProcessSettingDef(Component component, ComponentSettingDef settingDef)
    {
        var settingFieldInfo = component.GetType().GetField(settingDef.Name)!;
        var settingField = (ISetting) settingFieldInfo.GetValue(component)!;

        object settingValue;

        if (!settingField.Type.IsArray)
        {
            var converter = GetValueConverter(settingField.Type);
            settingValue = converter.Convert(settingDef.Node);
        }
        else
        {
            var arrayType = settingField.Type.GetElementType()!;
            var converter = GetValueConverter(arrayType);
            var childValues = settingDef.ArrayItems
                .Select(childDef =>
                {
                    var childConverter = childDef.TypeName != null
                        ? GetValueConverter(childDef.TypeName)
                        : converter;
                    return childConverter.Convert(childDef.Node);
                })
                .ToArray();
            var arrayValue = Array.CreateInstance(arrayType, childValues.Length);
            Array.Copy(childValues, arrayValue, childValues.Length);
            settingValue = arrayValue;
        }

        var settingType = typeof(Setting<>).MakeGenericType(settingField.Type);
        var setting = (ISetting) Activator.CreateInstance(settingType, settingValue)!;

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

    internal static bool HasAttribute(XmlNode node, string name)
    {
        return node.Attributes?[name]?.Value != null;
    }

    internal static string GetAttribute(XmlNode node, string name)
    {
        return node.Attributes?[name]?.Value ??
               throw new XmlSchemaException($"Node must have a \"{name}\" attribute.");
    }

    private Assembly[] GetLibraryAssemblies()
    {
        if (!_hasInstalledLoadingHook)
        {
            _hasInstalledLoadingHook = true;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        var assemblies = LibraryDefs
            .Select(def => Assembly.LoadFile(def.FilePath))
            .Concat(AppDomain.CurrentDomain.GetAssemblies())
            .Distinct()
            .ToArray();

        assemblies.ForEach(assembly => assembly.GetTypes());

        return assemblies;
    }

    private static Dictionary<string, Type> GetAssembliesComponentTypes(IEnumerable<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsAssignableTo(typeof(Component)))
            .Distinct()
            .ToDictionary(
                type => type.IsGenericType ? type.Name[..type.Name.IndexOf('`')] : type.Name,
                type => type);
    }

    private static Dictionary<string, Type> GetAssembliesArtifactTypes(IEnumerable<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsAssignableTo(typeof(IArtifact)))
            .Distinct()
            .ToDictionary(type => type.Name, type => type);
    }

    private Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var name = args.Name;

        if (name.Contains(".resources"))
        {
            return null;
        }

        var assembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.FullName == name);
        if (assembly != null)
        {
            return assembly;
        }

        var filename = $"{name.Split(',').First()}.dll";
        var lookupPaths = _libraryLookupPaths.Concat(new[] {Path.GetFullPath("./")});

        foreach (var lookupPath in lookupPaths)
        {
            var path = Path.Join(lookupPath, filename);
            try
            {
                return Assembly.LoadFrom(path);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        return null;
    }
}