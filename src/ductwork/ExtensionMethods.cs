using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

#nullable enable
namespace ductwork;

public static class ExtensionMethods
{
    internal record FieldResult<T>(FieldInfo Info, T Value);

    internal static IEnumerable<FieldResult<T>> GetFields<T>(this object obj)
    {
        return obj.GetType().GetFields()
            .Where(fieldInfo => fieldInfo.FieldType.IsAssignableTo(typeof(T)))
            .Select(fieldInfo => new FieldResult<T>(fieldInfo, (T) fieldInfo.GetValue(obj)!));
    }

    public static IEnumerable<T> NotNull<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.Where(item => item != null)!;
    }

    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> enumerable) where T : struct
    {
        return enumerable.Where(e => e != null).Select(e => e!.Value);
    }

    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var e in enumerable)
        {
            action(e);
        }
    }

    public static IEnumerable<XmlNode> SelectXPath(this XmlNode node, string xpath)
    {
        var nodes = node.SelectNodes(xpath);

        if (nodes == null)
        {
            yield break;
        }

        foreach (XmlNode child in nodes)
        {
            yield return child;
        }
    }
}