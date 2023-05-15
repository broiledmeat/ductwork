using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace ductwork.Builders.Xml;

public class ConnectionDef : NodeBackedDef
{
    private const string OutputAttr = "out";
    private const string InputAttr = "in";
    private const string DefaultOutputPlugName = "Out";
    private const string DefaultInputPlugName = "In";

    public ConnectionDef(XmlNode node) : base(node)
    {
    }

    public string OutputComponentName => GetComponentName(OutputAttr);
    public string OutputPlugName => GetPlugName(OutputAttr) ?? DefaultOutputPlugName;
    public string InputComponentName => GetComponentName(InputAttr);
    public string InputPlugName => GetPlugName(InputAttr) ?? DefaultInputPlugName;

    public override IEnumerable<Exception> Validate()
    {
        if (!XmlBuilder.HasAttribute(Node, OutputAttr))
        {
            yield return new XmlSchemaValidationException($"Node requires \"{OutputAttr}\" attribute.");
        }
        
        if (!XmlBuilder.HasAttribute(Node, InputAttr))
        {
            yield return new XmlSchemaValidationException($"Node requires \"{InputAttr}\" attribute.");
        }
    }

    private string GetComponentName(string attrName) => XmlBuilder
        .GetAttribute(Node, attrName)
        .Split('.')
        .First();

    private string? GetPlugName(string attrName) => XmlBuilder
        .GetAttribute(Node, attrName)
        .Split('.')
        .Skip(1)
        .FirstOrDefault();
}