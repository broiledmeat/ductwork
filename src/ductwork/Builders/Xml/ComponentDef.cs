using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace ductwork.Builders.Xml;

public record ComponentDef(XmlNode Node) : NodeBackedDef(Node)
{
    private const string NameAttr = "name";
    private const string TypeNameAttr = "type";

    public string Name => XmlBuilder.GetAttribute(Node, NameAttr);
    public string TypeName => XmlBuilder.GetAttribute(Node, TypeNameAttr);
    public string BaseTypeName => XmlBuilder.SplitTypeNames(TypeName).Item1;

    public IEnumerable<ComponentSettingDef> Settings => Node
        .SelectXPath("set")
        .Select(node => new ComponentSettingDef(node));

    public override IEnumerable<Exception> Validate()
    {
        if (!XmlBuilder.HasAttribute(Node, NameAttr))
        {
            yield return new XmlSchemaValidationException($"Node requires \"{NameAttr}\" attribute.");
        }

        if (!XmlBuilder.HasAttribute(Node, TypeNameAttr))
        {
            yield return new XmlSchemaValidationException($"Node requires \"{TypeNameAttr}\" attribute.");
        }
    }
}

public record ComponentSettingDef(XmlNode Node) : NodeBackedDef(Node)
{
    private const string NameAttr = "name";

    public string Name => XmlBuilder.GetAttribute(Node, NameAttr);
    public string RawValue => Node.InnerText;

    public IEnumerable<ArrayItemDef> ArrayItems => Node
        .SelectXPath("item")
        .Select(node => new ArrayItemDef(node));

    public override IEnumerable<Exception> Validate()
    {
        if (!XmlBuilder.HasAttribute(Node, NameAttr))
        {
            yield return new XmlSchemaValidationException($"Node requires \"{NameAttr}\" attribute.");
        }
    }
}