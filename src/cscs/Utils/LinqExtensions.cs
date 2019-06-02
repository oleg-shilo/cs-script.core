using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace csscript
{
    public static class LinqExtensions
    {
        public static List<T> AddIfNotThere<T>(this List<T> items, T item)
        {
            if (!items.Contains(item))
                items.Add(item);
            return items;
        }

        public static bool None<T>(this IEnumerable<T> items, Func<T, bool> predicate) => !items.Any(predicate);

        public static IEnumerable<TSource> AddItem<TSource>(this IEnumerable<TSource> items, TSource item) =>
            items.Concat(new[] { item });

        public static bool IsEmpty<T>(this IEnumerable<T> collection) => collection == null ? true : !collection.Any();

        public static string[] Distinct(this string[] list) => Enumerable.Distinct(list).ToArray();

        public static string[] ConcatWith(this string[] array1, IEnumerable<string> array2) =>
            array1.Concat(array2).ToArray();

        public static string[] ConcatWith(this string[] array, string item) =>
            array.Concat(new[] { item }).ToArray();

        public static string[] ConcatWith(this string item, IEnumerable<string> array)
        {
            return new[] { item }.Concat(array).ToArray();
        }

        /// <summary>
        /// A generic LINQ equivalent of C# foreach loop.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (T item in collection)
            {
                action(item);
            }
            return collection;
        }
    }
}