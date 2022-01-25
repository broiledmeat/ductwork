using System;
using System.Collections.Generic;
using System.Xml;

#nullable enable
namespace ductwork.Builders.Xml;

public abstract class NodeBackedDef
{
    public readonly XmlNode Node;

    public NodeBackedDef(XmlNode node)
    {
        Node = node;
    }

    public abstract IEnumerable<Exception> Validate();
}