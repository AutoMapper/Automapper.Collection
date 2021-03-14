using System;

namespace AutoMapper.Collection
{
    internal static class Extensions
    {
        internal static T Tap<T>(this T fluentExpression, Func<T, T> func) => func(fluentExpression);
    }
}
