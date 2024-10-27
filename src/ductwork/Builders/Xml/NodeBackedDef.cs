using System;
using System.Collections.Generic;
using System.Xml;

namespace ductwork.Builders.Xml;

public abstract record NodeBackedDef(XmlNode Node)
{
    public readonly XmlNode Node = Node;

    public abstract IEnumerable<Exception> Validate();
}