﻿//  This file is part of YamlDotNet - A .NET library for YAML.
//  Copyright (c) Antoine Aubry and contributors

//  Permission is hereby granted, free of charge, to any person obtaining a copy of
//  this software and associated documentation files (the "Software"), to deal in
//  the Software without restriction, including without limitation the rights to
//  use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//  of the Software, and to permit persons to whom the Software is furnished to do
//  so, subject to the following conditions:

//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.

//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace YamlDotNet
{
#if (UNITY)
    internal static class StandardRegexOptions
    {
        public const RegexOptions Compiled = RegexOptions.None;
    }
#else
    internal static class StandardRegexOptions
    {
        public const RegexOptions Compiled = RegexOptions.Compiled;
    }
#endif

#if NETSTANDARD1_3
    internal static class ReflectionExtensions
    {
        public static Type? BaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }

        public static bool IsValueType(this Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }

        public static bool IsGenericType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

        public static bool IsGenericTypeDefinition(this Type type)
        {
            return type.GetTypeInfo().IsGenericTypeDefinition;
        }

        public static bool IsInterface(this Type type)
        {
            return type.GetTypeInfo().IsInterface;
        }

        public static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        /// <summary>
        /// Determines whether the specified type has a default constructor.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     <c>true</c> if the type has a default constructor; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasDefaultConstructor(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsValueType || typeInfo.DeclaredConstructors
                .Any(c => c.IsPublic && !c.IsStatic && c.GetParameters().Length == 0);
        }

        public static bool IsAssignableFrom(this Type type, Type source)
        {
            return type.IsAssignableFrom(source.GetTypeInfo());
        }

        public static bool IsAssignableFrom(this Type type, TypeInfo source)
        {
            return type.GetTypeInfo().IsAssignableFrom(source);
        }

        public static TypeCode GetTypeCode(this Type type)
        {
            bool isEnum = type.IsEnum();
            if (isEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type == typeof(bool))
            {
                return TypeCode.Boolean;
            }
            else if (type == typeof(char))
            {
                return TypeCode.Char;
            }
            else if (type == typeof(sbyte))
            {
                return TypeCode.SByte;
            }
            else if (type == typeof(byte))
            {
                return TypeCode.Byte;
            }
            else if (type == typeof(short))
            {
                return TypeCode.Int16;
            }
            else if (type == typeof(ushort))
            {
                return TypeCode.UInt16;
            }
            else if (type == typeof(int))
            {
                return TypeCode.Int32;
            }
            else if (type == typeof(uint))
            {
                return TypeCode.UInt32;
            }
            else if (type == typeof(long))
            {
                return TypeCode.Int64;
            }
            else if (type == typeof(ulong))
            {
                return TypeCode.UInt64;
            }
            else if (type == typeof(float))
            {
                return TypeCode.Single;
            }
            else if (type == typeof(double))
            {
                return TypeCode.Double;
            }
            else if (type == typeof(decimal))
            {
                return TypeCode.Decimal;
            }
            else if (type == typeof(DateTime))
            {
                return TypeCode.DateTime;
            }
            else if (type == typeof(String))
            {
                return TypeCode.String;
            }
            else
            {
                return TypeCode.Object;
            }
        }

        public static bool IsDbNull(this object value)
        {
            return value?.GetType()?.FullName == "System.DBNull";
        }

        public static Type[] GetGenericArguments(this Type type)
        {
            return type.GetTypeInfo().GenericTypeArguments;
        }

        public static PropertyInfo? GetPublicProperty(this Type type, string name)
        {
            return type.GetRuntimeProperty(name);
        }

        public static FieldInfo? GetPublicStaticField(this Type type, string name)
        {
            return type.GetRuntimeField(name);
        }

        public static IEnumerable<ConstructorInfo> GetConstructors(this Type type)
        {
            return type.GetTypeInfo().DeclaredConstructors
                .Where(c => c.IsPublic && !c.IsStatic);
        }

        public static IEnumerable<PropertyInfo> GetPublicProperties(this Type type)
        {
            var instancePublic = new Func<PropertyInfo, bool>(
                p => !p.GetMethod.IsStatic && p.GetMethod.IsPublic);
            return type.IsInterface()
                ? (new Type[] { type })
                    .Concat(type.GetInterfaces())
                    .SelectMany(i => i.GetRuntimeProperties().Where(instancePublic))
                : type.GetRuntimeProperties().Where(instancePublic);
        }

        public static IEnumerable<FieldInfo> GetPublicFields(this Type type)
        {
            return type.GetRuntimeFields().Where(f => !f.IsStatic && f.IsPublic);
        }

        public static IEnumerable<MethodInfo> GetPublicStaticMethods(this Type type)
        {
            return type.GetRuntimeMethods()
                .Where(m => m.IsPublic && m.IsStatic);
        }

        public static MethodInfo GetPrivateStaticMethod(this Type type, string name)
        {
            return type.GetRuntimeMethods()
                .FirstOrDefault(m => !m.IsPublic && m.IsStatic && m.Name.Equals(name))
                ?? throw new MissingMethodException($"Expected to find a method named '{name}' in '{type.FullName}'.");
        }

        public static MethodInfo? GetPublicStaticMethod(this Type type, string name, params Type[] parameterTypes)
        {
            return type.GetRuntimeMethods()
                .FirstOrDefault(m =>
                {
                    if (m.IsPublic && m.IsStatic && m.Name.Equals(name))
                    {
                        var parameters = m.GetParameters();
                        return parameters.Length == parameterTypes.Length
                            && parameters.Zip(parameterTypes, (pi, pt) => pi.ParameterType == pt).All(r => r);
                    }
                    return false;
                });
        }

        public static MethodInfo? GetPublicInstanceMethod(this Type type, string name)
        {
            return type.GetRuntimeMethods()
                .FirstOrDefault(m => m.IsPublic && !m.IsStatic && m.Name.Equals(name));
        }

        public static MethodInfo GetGetMethod(this PropertyInfo property)
        {
            return property.GetMethod;
        }

        public static MethodInfo GetSetMethod(this PropertyInfo property)
        {
            return property.SetMethod;
        }

        public static IEnumerable<Type> GetInterfaces(this Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces;
        }

        public static Exception Unwrap(this TargetInvocationException ex)
        {
            return ex.InnerException;
        }

        public static bool IsInstanceOf(this Type type, object o)
        {
            return o.GetType() == type || o.GetType().GetTypeInfo().IsSubclassOf(type);
        }

        public static Attribute[] GetAllCustomAttributes<TAttribute>(this PropertyInfo member)
        {
            // IMemberInfo.GetCustomAttributes ignores it's "inherit" parameter for properties,
            // and the suggested replacement (Attribute.GetCustomAttributes) is not available
            // on netstandard1.3
            var result = new List<Attribute>();
            var type = member.DeclaringType;

            while (type != null)
            {
                type.GetPublicProperty(member.Name);
                result.AddRange(member.GetCustomAttributes(typeof(TAttribute)));

                type = type.BaseType();
            }

            return result.ToArray();
        }
    }

    internal sealed class CultureInfoAdapter : CultureInfo
    {
        private readonly IFormatProvider provider;

        public CultureInfoAdapter(CultureInfo baseCulture, IFormatProvider provider)
            : base(baseCulture.Name)
        {
            this.provider = provider;
        }

        public override object GetFormat(Type formatType)
        {
            return provider.GetFormat(formatType);
        }
    }
#else

    internal static class ReflectionExtensions
    {
        public static Type? BaseType(this Type type)
        {
            return type.BaseType;
        }

        public static bool IsValueType(this Type type)
        {
            return type.IsValueType;
        }

        public static bool IsGenericType(this Type type)
        {
            return type.IsGenericType;
        }

        public static bool IsGenericTypeDefinition(this Type type)
        {
            return type.IsGenericTypeDefinition;
        }

        public static bool IsInterface(this Type type)
        {
            return type.IsInterface;
        }

        public static bool IsEnum(this Type type)
        {
            return type.IsEnum;
        }

        public static bool IsDbNull(this object value)
        {
            return value is DBNull;
        }

        /// <summary>
        /// Determines whether the specified type has a default constructor.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     <c>true</c> if the type has a default constructor; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasDefaultConstructor(this Type type)
        {
            return type.IsValueType || type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) != null;
        }

        public static TypeCode GetTypeCode(this Type type)
        {
            return Type.GetTypeCode(type);
        }

        public static PropertyInfo? GetPublicProperty(this Type type, string name)
        {
            return type.GetProperty(name);
        }

        public static FieldInfo? GetPublicStaticField(this Type type, string name)
        {
            return type.GetField(name, BindingFlags.Static | BindingFlags.Public);
        }

        public static IEnumerable<PropertyInfo> GetPublicProperties(this Type type)
        {
            var instancePublic = BindingFlags.Instance | BindingFlags.Public;
            return type.IsInterface
                ? (new Type[] { type })
                    .Concat(type.GetInterfaces())
                    .SelectMany(i => i.GetProperties(instancePublic))
                : type.GetProperties(instancePublic);
        }

        public static IEnumerable<FieldInfo> GetPublicFields(this Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public);
        }

        public static IEnumerable<MethodInfo> GetPublicStaticMethods(this Type type)
        {
            return type.GetMethods(BindingFlags.Static | BindingFlags.Public);
        }

        public static MethodInfo GetPrivateStaticMethod(this Type type, string name)
        {
            return type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new MissingMethodException($"Expected to find a method named '{name}' in '{type.FullName}'.");
        }

        public static MethodInfo? GetPublicStaticMethod(this Type type, string name, params Type[] parameterTypes)
        {
            return type.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameterTypes, null);
        }

        public static MethodInfo? GetPublicInstanceMethod(this Type type, string name)
        {
            return type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
        }

        private static readonly FieldInfo? remoteStackTraceField = typeof(Exception)
                .GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);

        public static Exception Unwrap(this TargetInvocationException ex)
        {
            var result = ex.InnerException;
            if (result == null)
            {
                return ex;
            }

            if (remoteStackTraceField != null)
            {
                remoteStackTraceField.SetValue(result, result.StackTrace + "\r\n");
            }
            return result;
        }

        public static bool IsInstanceOf(this Type type, object o)
        {
            return type.IsInstanceOfType(o);
        }

        public static Attribute[] GetAllCustomAttributes<TAttribute>(this PropertyInfo property)
        {
            // Don't use IMemberInfo.GetCustomAttributes, it ignores the inherit parameter
            return Attribute.GetCustomAttributes(property, typeof(TAttribute));
        }
    }

    internal sealed class CultureInfoAdapter : CultureInfo
    {
        private readonly IFormatProvider provider;

        public CultureInfoAdapter(CultureInfo baseCulture, IFormatProvider provider)
            : base(baseCulture.LCID)
        {
            this.provider = provider;
        }

        public override object? GetFormat(Type? formatType)
        {
            return provider.GetFormat(formatType);
        }
    }

#endif

#if UNITY
    internal static class PropertyInfoExtensions
    {
        public static object? ReadValue(this PropertyInfo property, object target)
        {
            return property.GetGetMethod().Invoke(target, null);
        }
    }
#else
    internal static class PropertyInfoExtensions
    {
        public static object? ReadValue(this PropertyInfo property, object target)
        {
            return property.GetValue(target, null);
        }
    }
#endif
}

#if NET20
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    internal sealed class ExtensionAttribute : Attribute { }
}

namespace System // To allow these to be public without clashing with the standard ones on platforms > 2.0
{
    public delegate TResult Func<TArg, TResult>(TArg arg);
    public delegate TResult Func<TArg1, TArg2, TResult>(TArg1 arg1, TArg2 arg2);
    public delegate TResult Func<TArg1, TArg2, TArg3, TResult>(TArg1 arg1, TArg2 arg2, TArg3 arg3);
}

namespace System.Linq.Expressions
{
    // Do not remove.
    // Avoids code breaking on .NET 2.0 due to using System.Linq.Expressions.
}

namespace System.Linq
{
    internal static partial class Enumerable
    {
        public static List<T> ToList<T>(this IEnumerable<T> sequence)
        {
            return new List<T>(sequence);
        }

        public static T[] ToArray<T>(this IEnumerable<T> sequence)
        {
            return sequence.ToList().ToArray();
        }

        public static IEnumerable<T> OrderBy<T, TKey>(this IEnumerable<T> sequence, Func<T, TKey> orderBy)
        {
            var comparer = Comparer<TKey>.Default;
            var list = sequence.ToList();
            list.Sort((a, b) => comparer.Compare(orderBy(a), orderBy(b)));
            return list;
        }

        public static IEnumerable<T> Empty<T>()
        {
            yield break;
        }

        public static IEnumerable<T> Where<T>(this IEnumerable<T> sequence, Func<T, bool> predicate)
        {
            foreach (var item in sequence)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<T2> Select<T1, T2>(this IEnumerable<T1> sequence, Func<T1, T2> selector)
        {
            foreach (var item in sequence)
            {
                yield return selector(item);
            }
        }

        public static IEnumerable<T> OfType<T>(this IEnumerable sequence)
        {
            foreach (var item in sequence)
            {
                if (item is T)
                {
                    yield return (T)item;
                }
            }
        }

        public static IEnumerable<T> Cast<T>(this IEnumerable sequence)
        {
            foreach (var item in sequence)
            {
                yield return (T)item;
            }
        }

        public static IEnumerable<T2> SelectMany<T1, T2>(this IEnumerable<T1> sequence, Func<T1, IEnumerable<T2>> selector)
        {
            foreach (var item in sequence)
            {
                foreach (var subitem in selector(item))
                {
                    yield return subitem;
                }
            }
        }

        public static T First<T>(this IEnumerable<T> sequence)
        {
            foreach (var item in sequence)
            {
                return item;
            }
            throw new InvalidOperationException();
        }

        public static T FirstOrDefault<T>(this IEnumerable<T> sequence)
        {
            foreach (var item in sequence)
            {
                return item;
            }
            return default!;
        }

        public static T SingleOrDefault<T>(this IEnumerable<T> sequence)
        {
            using (var enumerator = sequence.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    return default!;
                }
                var result = enumerator.Current;
                if (enumerator.MoveNext())
                {
                    throw new InvalidOperationException();
                }
                return result;
            }
        }

        public static T Single<T>(this IEnumerable<T> sequence)
        {
            using (var enumerator = sequence.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    throw new InvalidOperationException();
                }
                var result = enumerator.Current;
                if (enumerator.MoveNext())
                {
                    throw new InvalidOperationException();
                }
                return result;
            }
        }

        public static T FirstOrDefault<T>(this IEnumerable<T> sequence, Func<T, bool> predicate)
        {
            return sequence.Where(predicate).FirstOrDefault();
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> first, IEnumerable<T> second)
        {
            foreach (var item in first)
            {
                yield return item;
            }
            foreach (var item in second)
            {
                yield return item;
            }
        }

        public static IEnumerable<T> Skip<T>(this IEnumerable<T> sequence, int skipCount)
        {
            foreach (var item in sequence)
            {
                if (skipCount <= 0)
                {
                    yield return item;
                }
                else
                {
                    --skipCount;
                }
            }
        }

        public static IEnumerable<T> SkipWhile<T>(this IEnumerable<T> sequence, Func<T, bool> predicate)
        {
            var skip = true;
            foreach (var item in sequence)
            {
                skip = skip && predicate(item);
                if (!skip)
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<T> TakeWhile<T>(this IEnumerable<T> sequence, Func<T, bool> predicate)
        {
            var take = true;
            foreach (var item in sequence)
            {
                take = take && predicate(item);
                if (take)
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<T> DefaultIfEmpty<T>(this IEnumerable<T> sequence, T defaultValue)
        {
            var isEmpty = true;
            foreach (var item in sequence)
            {
                yield return item;
                isEmpty = false;
            }
            if (isEmpty)
            {
                yield return defaultValue;
            }
        }

        public static bool Any<T>(this IEnumerable<T> sequence)
        {
            using var enumerator = sequence.GetEnumerator();
            return enumerator.MoveNext();
        }

        public static bool Any<T>(this IEnumerable<T> sequence, Func<T, bool> predicate)
        {
            return sequence.Where(predicate).Any();
        }

        public static int Count<T>(this IEnumerable<T> sequence)
        {
            var count = 0;
            foreach (var item in sequence)
            {
                ++count;
            }
            return count;
        }

        public static bool Contains<T>(this IEnumerable<T> sequence, T value)
        {
            foreach (var item in sequence)
            {
                if (Equals(item, value))
                {
                    return true;
                }
            }
            return false;
        }

        public static TSource Aggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func)
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    throw new InvalidOperationException();
                }
                var accumulator = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    accumulator = func(accumulator, enumerator.Current);
                }
                return accumulator;
            }
        }

        public static TAccumulate Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
        {
            var accumulator = seed;
            foreach (var item in source)
            {
                accumulator = func(accumulator, item);
            }
            return accumulator;
        }

        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return source.ToLookup(keySelector, e => e);
        }

        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            var lookup = new Lookup<TKey, TElement>();
            foreach (var item in source)
            {
                lookup.Add(keySelector(item), elementSelector(item));
            }
            return lookup;
        }

        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return source.ToLookup(keySelector);
        }

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector)
        {
            foreach (var group in source.ToLookup(keySelector))
            {
                yield return resultSelector(group.Key, group);
            }
        }

        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return source.ToDictionary(keySelector, e => e);
        }

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            var result = new Dictionary<TKey, TElement>();
            foreach (var item in source)
            {
                result.Add(keySelector(item), elementSelector(item));
            }
            return result;
        }
    }

    internal interface ILookup<TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>, IEnumerable
    {
        IEnumerable<TElement> this[TKey key] { get; }
        int Count { get; }
        bool Contains(TKey key);
    }

    internal interface IGrouping<out TKey, TElement> : IEnumerable<TElement>, IEnumerable
    {
        TKey Key { get; }
    }

    internal sealed class Lookup<TKey, TElement> : ILookup<TKey, TElement>
    {
        private readonly Dictionary<TKey, List<TElement>> entries = new Dictionary<TKey, List<TElement>>();
        private readonly List<TKey> keys = new List<TKey>();

        public int Count => entries.Count;

        private sealed class Grouping : IGrouping<TKey, TElement>
        {
            private readonly IEnumerable<TElement> elements;

            public TKey Key { get; }

            public Grouping(TKey key, IEnumerable<TElement> elements)
            {
                Key = key;
                this.elements = elements;
            }

            public IEnumerator<TElement> GetEnumerator() => elements.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public void Add(TKey key, TElement element)
        {
            if (!entries.TryGetValue(key, out var group))
            {
                keys.Add(key);
                group = new List<TElement>();
                entries.Add(key, group);
            }
            group.Add(element);
        }

        public IEnumerable<TElement> this[TKey key]
        {
            get
            {
                return entries.TryGetValue(key, out var elements) ? elements : Enumerable.Empty<TElement>();
            }
        }

        public bool Contains(TKey key) => entries.ContainsKey(key);

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            foreach (var key in keys)
            {
                yield return new Grouping(key, entries[key]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

namespace System.Collections.Generic
{
    internal class HashSet<T> : IEnumerable<T>
    {
        private readonly Dictionary<T, object?> items;

        public HashSet()
        {
            items = new Dictionary<T, object?>();
        }

        public HashSet(IEqualityComparer<T> comparer)
        {
            items = new Dictionary<T, object?>(comparer);
        }

        public bool Add(T value)
        {
            if (Contains(value))
            {
                return false;
            }
            else
            {
                items.Add(value, null);
                return true;
            }
        }

        public bool Contains(T value)
        {
            return items.ContainsKey(value);
        }

        public void Clear()
        {
            items.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return items.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
#endif

#if UNITY
namespace System.Runtime.Versioning
{
    // Unity uses the net40 target, because that simplifies the project configuration.
    // But in practice it targets framework 3.5, which does not have the TargetFrameworkAttribute
    // that is added to the generated AssemblyInfo.
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    internal sealed class TargetFrameworkAttribute : Attribute
    {
        public string FrameworkName { get; set; }
        public string? FrameworkDisplayName { get; set; }

        public TargetFrameworkAttribute(string frameworkName)
        {
            FrameworkName = frameworkName;
        }
    }
}
#endif

#if NET20 || NET35 || UNITY
namespace System.Linq
{
    internal static partial class Enumerable2
    {
        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            using var firstEnumerator = first.GetEnumerator();
            using var secondEnumerator = second.GetEnumerator();
            while (firstEnumerator.MoveNext() && secondEnumerator.MoveNext())
            {
                yield return resultSelector(firstEnumerator.Current, secondEnumerator.Current);
            }
        }
    }
}

namespace System.Collections.Concurrent
{
    internal sealed class ConcurrentDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> entries = new Dictionary<TKey, TValue>();

        public delegate TValue ValueFactory(TKey key);

        public TValue GetOrAdd(TKey key, ValueFactory valueFactory)
        {
            lock (entries)
            {
                if (!entries.TryGetValue(key, out var value))
                {
                    value = valueFactory(key);
                    entries.Add(key, value);
                }
                return value;
            }
        }
    }
}

namespace System.Collections.Generic
{
    public interface IReadOnlyCollection<T> : IEnumerable<T>, IEnumerable
    {
        int Count { get; }
    }

    public interface IReadOnlyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IReadOnlyCollection<KeyValuePair<TKey, TValue>> where TKey : notnull
    {
        TValue this[TKey key] { get; }
        IEnumerable<TKey> Keys { get; }
        IEnumerable<TValue> Values { get; }
        bool ContainsKey(TKey key);
        bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);

    }

    public interface IReadOnlyList<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
    {
        T this[int index] { get; }
    }
}

namespace YamlDotNet.Helpers
{
    public static class ReadOnlyCollectionExtensions
    {
        private sealed class ReadOnlyListAdapter<T> : IReadOnlyList<T>
        {
            private readonly List<T> list;

            public ReadOnlyListAdapter(List<T> list)
            {
                this.list = list ?? throw new ArgumentNullException(nameof(list));
            }

            public T this[int index] => list[index];
            public int Count => list.Count;
            public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
        }

        public static IReadOnlyList<T> AsReadonlyList<T>(this List<T> list)
        {
            return new ReadOnlyListAdapter<T>(list);
        }

        private sealed class ReadOnlyDictionaryAdapter<TKey, TValue> : IReadOnlyDictionary<TKey, TValue> where TKey : notnull
        {
            private readonly Dictionary<TKey, TValue> dictionary;

            public ReadOnlyDictionaryAdapter(Dictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            }

            public TValue this[TKey key] => dictionary[key];

            public IEnumerable<TKey> Keys => dictionary.Keys;

            public IEnumerable<TValue> Values => dictionary.Values;

            public int Count => dictionary.Count;

            public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();

            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => dictionary.TryGetValue(key, out value);

            IEnumerator IEnumerable.GetEnumerator() => dictionary.GetEnumerator();
        }

        public static IReadOnlyDictionary<TKey, TValue> AsReadonlyDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) where TKey : notnull
        {
            return new ReadOnlyDictionaryAdapter<TKey, TValue>(dictionary);
        }
    }
}
#else
namespace YamlDotNet.Helpers
{
    public static class ReadOnlyCollectionExtensions
    {
        public static IReadOnlyList<T> AsReadonlyList<T>(this List<T> list)
        {
            return list;
        }

        public static IReadOnlyDictionary<TKey, TValue> AsReadonlyDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) where TKey : notnull
        {
            return dictionary;
        }
    }
}
#endif

#if NET20 || NET35 || NET45 || UNITY || NETSTANDARD1_3
namespace System.Collections.Generic
{
    internal static class DeconstructionExtensions
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }
    }
}
#endif