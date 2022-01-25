using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

#nullable enable
namespace ductwork.Builders.Xml;

public class ComponentDef : NodeBackedDef
{
    private const string NameAttr = "name";
    private const string TypeNameAttr = "type";

    public ComponentDef(XmlNode node) : base(node)
    {
    }

    public string Name => XmlBuilder.GetAttribute(Node, NameAttr);
    public string TypeName => XmlBuilder.GetAttribute(Node, TypeNameAttr);

    public IEnumerable<ComponentSettingDef> Settings => Node
        .SelectXPath("set")
        .Select(node => new ComponentSettingDef(node));

    public override IEnumerable<Exception> Validate()
    {
        if (!XmlBuilder.HasAttribute(Node, NameAttr))
        {
            yield return new XmlSchemaValidationException($"Node requires {NameAttr} attribute.");
        }

        if (!XmlBuilder.HasAttribute(Node, TypeNameAttr))
        {
            yield return new XmlSchemaValidationException($"Node requires {TypeNameAttr} attribute.");
        }
    }
}

public class ComponentSettingDef : NodeBackedDef
{
    private const string NameAttr = "name";

    public ComponentSettingDef(XmlNode node) : base(node)
    {
    }

    public string Name => XmlBuilder.GetAttribute(Node, NameAttr);
    public string RawValue => Node.InnerText;

    public IEnumerable<ArrayItemDef> ArrayItems => Node
        .SelectXPath("item")
        .Select(node => new ArrayItemDef(node));

    public override IEnumerable<Exception> Validate()
    {
        if (!XmlBuilder.HasAttribute(Node, NameAttr))
        {
            yield return new XmlSchemaValidationException($"Node requires {NameAttr} attribute.");
        }
    }
}