using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace ductwork.Builders.Xml;

public record LibraryDef(XmlNode Node) : NodeBackedDef(Node)
{
    private const string PathAttr = "path";

    public string FilePath => Path.GetFullPath(XmlBuilder.GetAttribute(Node, PathAttr));

    public override IEnumerable<Exception> Validate()
    {
        if (!XmlBuilder.HasAttribute(Node, PathAttr))
        {
            yield return new XmlSchemaValidationException($"Node requires \"{PathAttr}\" attribute.");
        }
    }
}