using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoMapper.Collection
{
    internal static class TypeHelper
    {
        public static Type GetElementType(Type enumerableType) => GetElementTypes(enumerableType, null)[0];

        private static Type[] GetElementTypes(Type enumerableType, IEnumerable enumerable,
            ElementTypeFlags flags = ElementTypeFlags.None)
        {
            if (enumerableType.HasElementType)
            {
                return new[] { enumerableType.GetElementType() };
            }

            if (flags.HasFlag(ElementTypeFlags.BreakKeyValuePair) && enumerableType.IsGenericType() &&
                enumerableType.IsDictionaryType())
            {
                return enumerableType.GetTypeInfo().GenericTypeArguments;
            }

            if (enumerableType.IsGenericType() &&
                enumerableType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return enumerableType.GetTypeInfo().GenericTypeArguments;
            }

            Type ienumerableType = GetIEnumerableType(enumerableType);
            if (ienumerableType != null)
            {
                return ienumerableType.GetTypeInfo().GenericTypeArguments;
            }

            if (typeof(IEnumerable).IsAssignableFrom(enumerableType))
            {
                var first = enumerable?.Cast<object>().FirstOrDefault();

                return new[] { first?.GetType() ?? typeof(object) };
            }

            throw new ArgumentException($"Unable to find the element type for type '{enumerableType}'.",
                nameof(enumerableType));
        }
        
        private static Type GetIEnumerableType(Type enumerableType)
        {
            try
            {
                return enumerableType.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(t => t.Name == "IEnumerable`1");
            }
            catch (AmbiguousMatchException)
            {
                return enumerableType.BaseType() != typeof(object) ? GetIEnumerableType(enumerableType.BaseType()) : null;
            }
        }
    }

    public enum ElementTypeFlags
    {
        None = 0,
        BreakKeyValuePair = 1
    }
}