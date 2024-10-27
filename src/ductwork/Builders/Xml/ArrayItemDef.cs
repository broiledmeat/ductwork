using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ductwork.Builders.Xml;

public record ArrayItemDef(XmlNode Node) : NodeBackedDef(Node)
{
    private const string TypeNameAttr = "type";

    public string? TypeName => XmlBuilder.HasAttribute(Node, TypeNameAttr)
        ? XmlBuilder.GetAttribute(Node, TypeNameAttr)
        : null;

    public override IEnumerable<Exception> Validate()
    {
        return [];
    }
}