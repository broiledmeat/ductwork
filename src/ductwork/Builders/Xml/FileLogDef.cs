using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using NLog;

#nullable enable
namespace ductwork.Builders.Xml;

public class FileLogDef : NodeBackedDef
{
    private const string PathAttr = "path";
    private const string MinAttr = "min";
    private const string MaxAttr = "max";

    public FileLogDef(XmlNode node) : base(node)
    {
    }

    public string FilePath => Path.GetFullPath(XmlBuilder.GetAttribute(Node, PathAttr));

    public LogLevel MinLevel => XmlBuilder.HasAttribute(Node, MinAttr)
        ? LogLevel.FromString(XmlBuilder.GetAttribute(Node, MinAttr))
        : LogLevel.Debug;

    public LogLevel MaxLevel => XmlBuilder.HasAttribute(Node, MaxAttr)
        ? LogLevel.FromString(XmlBuilder.GetAttribute(Node, MaxAttr))
        : LogLevel.Fatal;

    public override IEnumerable<Exception> Validate()
    {
        if (!XmlBuilder.HasAttribute(Node, PathAttr))
        {
            yield return new XmlSchemaValidationException($"Node requires {PathAttr} attribute.");
        }

        if (XmlBuilder.HasAttribute(Node, MinAttr))
        {
            Exception? exception = null;

            try
            {
                LogLevel.FromString(XmlBuilder.GetAttribute(Node, MinAttr));
            }
            catch (ArgumentException e)
            {
                exception = e;
            }

            if (exception != null)
            {
                yield return exception;
            }
        }

        if (XmlBuilder.HasAttribute(Node, MaxAttr))
        {
            Exception? exception = null;

            try
            {
                LogLevel.FromString(XmlBuilder.GetAttribute(Node, MaxAttr));
            }
            catch (ArgumentException e)
            {
                exception = e;
            }

            if (exception != null)
            {
                yield return exception;
            }
        }
    }
}