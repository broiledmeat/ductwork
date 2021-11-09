using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Xml;
using System.Xml.Schema;

#nullable enable
namespace ductwork.FileLoaders
{
    public static class GraphXmlLoader
    {
        private record ValueConverter(Type Type, Func<XmlNode, object> Convert);

        private static readonly Dictionary<string, ValueConverter> ValueConverters = new()
        {
            {"string", new ValueConverter(typeof(string), node => node.InnerText.Trim())},
            {"int", new ValueConverter(typeof(int), node => Convert.ToInt32(node.InnerText.Trim()))},
            {"float", new ValueConverter(typeof(float), node => Convert.ToSingle(node.InnerText.Trim()))},
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

            Dictionary<string, Component> components = document
                .SelectXPath("/graph/component")
                .Select(node => ProcessComponentNode(node, componentTypes))
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            components.Values
                .ForEach(component => graph.Add(component));

            document
                .SelectXPath("/graph/connection")
                .Select(node => ProcessConnectionNode(node, components))
                .ForEach(pair => graph.Connect(pair.Item1, pair.Item2));

            return graph;
        }

        private static Assembly ProcessLibNode(XmlNode node)
        {
            var path = node.Attributes?["path"]?.Value;

            if (path == null)
            {
                throw new InvalidOperationException("Lib node must have a path attribute.");
            }

            var fullPath = Path.GetFullPath(path);
            return Assembly.LoadFrom(fullPath);
        }

        private static KeyValuePair<string, Component> ProcessComponentNode(
            XmlNode node,
            IReadOnlyDictionary<string, Type> componentTypes)
        {
            var key = node.Attributes?["key"]?.Value;
            var fullTypeName = node.Attributes?["type"]?.Value;

            if (key == null)
            {
                throw new XmlSchemaException("Component node must have a key attribute.");
            }

            if (fullTypeName == null)
            {
                throw new XmlSchemaException("Component node must have a type attribute.");
            }

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
                var converter = GetValueConverter(subTypeName);
                componentType = componentType.MakeGenericType(converter.Type);
            }

            var componentConstructor = componentType.GetConstructor(componentArgTypes);

            if (componentConstructor == null)
            {
                var argNames = string.Join(", ", componentArgTypes.Select(type => type.Name));
                throw new InvalidOperationException($"Could not find Component constructor for {argNames}");
            }

            var component = (Component) componentConstructor.Invoke(componentArgs);
            return new KeyValuePair<string, Component>(key, component);
        }

        private static (IOutputPlug, IInputPlug) ProcessConnectionNode(
            XmlNode node,
            Dictionary<string, Component> components)
        {
            var outName = node.Attributes?["out"]?.Value;
            var inName = node.Attributes?["in"]?.Value;

            if (outName == null)
            {
                throw new XmlSchemaException("Connection node must have an out attribute.");
            }

            if (inName == null)
            {
                throw new XmlSchemaException("Connection node must have an in attribute.");
            }

            var (outComponentName, outPlugName) = SplitConnectionNames(outName);
            var (inComponentName, inPlugName) = SplitConnectionNames(inName);

            var outComponent = components[outComponentName];
            var inComponent = components[inComponentName];

            var output = (IOutputPlug) outComponent.GetType().GetField(outPlugName)!.GetValue(outComponent)!;
            var input = (IInputPlug) inComponent.GetType().GetField(inPlugName)!.GetValue(inComponent)!;

            return (output, input);
        }

        private static object ProcessArgNode(XmlNode node)
        {
            var typeName = node.Attributes?["type"]?.Value;

            if (typeName == null)
            {
                throw new XmlSchemaException("Variable node must have a type attribute.");
            }

            var (mainTypeName, subTypeName) = SplitTypeNames(typeName);

            if (mainTypeName != "array")
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
    }
}