using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PdfSharpDslCore.Evaluation
{
    /// <summary>
    /// Field access on host data (<c>$order.customer</c>): dictionaries by key, other objects by public property or
    /// field. Names are matched case-insensitively, an exact match wins.
    /// </summary>
    public static class PdfMembers
    {
        /// <summary>True for the values that are read by field name (dictionaries), and so are not lists even though they enumerate.</summary>
        public static bool IsDictionary(object? value)
        {
            if (value is null || value is string) return false;
            if (value is IDictionary) return true;
            return value.GetType().GetInterfaces().Any(i => i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IDictionary<,>) || i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));
        }

        /// <summary>Reads the field <paramref name="name"/> of <paramref name="target"/>; false when it has no such field.</summary>
        public static bool TryGet(object target, string name, out object? value)
        {
            if (IsDictionary(target))
            {
                foreach (var entry in Entries(target))
                {
                    if (string.Equals(entry.Key, name, StringComparison.Ordinal))
                    {
                        value = entry.Value;
                        return true;
                    }
                }

                foreach (var entry in Entries(target))
                {
                    if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = entry.Value;
                        return true;
                    }
                }

                value = null;
                return false;
            }

            var type = target.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var property = type.GetProperty(name, flags | BindingFlags.IgnoreCase);
            if (property != null && property.GetIndexParameters().Length == 0 && property.CanRead)
            {
                value = property.GetValue(target);
                return true;
            }

            var field = type.GetField(name, flags | BindingFlags.IgnoreCase);
            if (field != null)
            {
                value = field.GetValue(target);
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>Names of the fields <paramref name="target"/> can be read with.</summary>
        public static IEnumerable<string> Names(object target)
        {
            if (IsDictionary(target)) return Entries(target).Select(e => e.Key).ToList();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var type = target.GetType();
            return type.GetProperties(flags).Where(p => p.GetIndexParameters().Length == 0 && p.CanRead).Select(p => p.Name)
                .Concat(type.GetFields(flags).Select(f => f.Name)).ToList();
        }

        private static IEnumerable<KeyValuePair<string, object?>> Entries(object dictionary)
        {
            if (dictionary is IDictionary plain)
            {
                foreach (DictionaryEntry entry in plain)
                {
                    yield return new KeyValuePair<string, object?>(Convert.ToString(entry.Key) ?? string.Empty, entry.Value);
                }

                yield break;
            }

            // IReadOnlyDictionary<,> or IDictionary<,> that is not a non-generic IDictionary (e.g. ExpandoObject)
            foreach (var item in (IEnumerable)dictionary)
            {
                var itemType = item.GetType();
                var key = itemType.GetProperty("Key")?.GetValue(item);
                var value = itemType.GetProperty("Value")?.GetValue(item);
                yield return new KeyValuePair<string, object?>(Convert.ToString(key) ?? string.Empty, value);
            }
        }
    }
}
