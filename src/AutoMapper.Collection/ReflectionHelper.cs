using System;
using System.Reflection;

namespace AutoMapper.Collection
{
    internal static class ReflectionHelper
    {
        public static object GetMemberValue(this MemberInfo propertyOrField, object target)
        {
            if (propertyOrField is PropertyInfo property) return property.GetValue(target, null);

            if (propertyOrField is FieldInfo field) return field.GetValue(target);

            throw new ArgumentOutOfRangeException(nameof(propertyOrField),
                "Expected a property or field, not " + propertyOrField?.MemberType);
        }

#if CSv8        // sorry I can't find programmatic way to do this for arbitrary C# installation
        public static Type GetMemberType(this MemberInfo memberInfo) =>
            memberInfo switch
            {
                MethodInfo mi => mi.ReturnType,
                PropertyInfo pi => pi.PropertyType,
                FieldInfo fi => fi.FieldType,
                _ => null
            };
#else
        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            //switch (memberInfo)       // this is my #2 choice. collaborators: delete whatever you don't like !
            //{
            //    case MethodInfo mi:
            //        return mi.ReturnType;
            //    case PropertyInfo pi:
            //        return pi.PropertyType;
            //    case FieldInfo fi:
            //        return fi.FieldType;
            //    default:
            //        return null;
            //}
            if (memberInfo is MethodInfo mi) return mi.ReturnType;

            if (memberInfo is PropertyInfo pi) return pi.PropertyType;

            if (memberInfo is FieldInfo fi) return fi.FieldType;

            return null;
        }
#endif
    }
}