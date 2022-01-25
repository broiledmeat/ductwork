using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

#nullable enable
namespace ductwork.Builders.Xml;

public class ArrayItemDef : NodeBackedDef
{
    private const string TypeNameAttr = "type";

    public ArrayItemDef(XmlNode node) : base(node)
    {
    }

    public string? TypeName => XmlBuilder.HasAttribute(Node, TypeNameAttr)
        ? XmlBuilder.GetAttribute(Node, TypeNameAttr)
        : null;

    public override IEnumerable<Exception> Validate()
    {
        return Enumerable.Empty<Exception>();
    }
}