namespace AutoMapper.Collection
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    internal static class PrimitiveExtensions
    {
        public static bool IsCollectionType(this Type type) => type.ImplementsGenericInterface(typeof(ICollection<>));
        public static bool IsDictionaryType(this Type type) => type.ImplementsGenericInterface(typeof(IDictionary<,>));
        public static bool IsEnumerableType(this Type type) => typeof(IEnumerable).IsAssignableFrom(type);

        private static bool ImplementsGenericInterface(this Type type, Type interfaceType)
        {
            if (type.IsGenericType(interfaceType)) return true;
            foreach (var @interface in type.GetTypeInfo().ImplementedInterfaces)
            {
                if (@interface.IsGenericType(interfaceType)) return true;
            }
            return false;
        }

        private static bool IsGenericType(this Type type, Type genericType) => type.IsGenericType() && type.GetGenericTypeDefinition() == genericType;
    }
}