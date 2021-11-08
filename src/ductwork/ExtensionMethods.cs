using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

#nullable enable
namespace ductwork
{
    public static class ExtensionMethods
    {
        public static IEnumerable<T> NotNull<T>(this IEnumerable<T> sequence)
        {
            return sequence.Where(item => item != null)!;
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> sequence) where T : struct
        {
            return sequence.Where(e => e != null).Select(e => e!.Value);
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var e in enumerable)
            {
                action(e);
            }
        }

        public static void AddEnumerable<T>(this ICollection<T> enumerable, IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                enumerable.Add(item);
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
}