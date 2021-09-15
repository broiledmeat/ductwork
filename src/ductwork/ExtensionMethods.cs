using System.Collections.Generic;
using System.Linq;

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

        public static void AddEnumerable<T>(this ICollection<T> enumerable, IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                enumerable.Add(item);
            }
        }
    }
}