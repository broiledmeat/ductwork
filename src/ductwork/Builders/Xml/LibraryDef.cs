using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;

#nullable enable
namespace ductwork.Builders.Xml;

public class LibraryDef : NodeBackedDef
{
    private const string PathAttr = "path";
    
    public LibraryDef(XmlNode node) : base(node)
    {
    }

    public string FilePath => Path.GetFullPath(XmlBuilder.GetAttribute(Node, PathAttr));
    
    public override IEnumerable<Exception> Validate()
    {
        if (!XmlBuilder.HasAttribute(Node, PathAttr))
        {
            yield return new XmlSchemaValidationException($"Node requires \"{PathAttr}\" attribute.");
        }
    }
}