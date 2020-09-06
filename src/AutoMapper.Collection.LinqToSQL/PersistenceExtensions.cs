using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using AutoMapper.Extensions.ExpressionMapping.Impl;

namespace AutoMapper.Collection.LinqToSQL
{
    public static class PersistenceExtensions
    {
        /// <summary>
        /// Obsolete: Use Persist(IMapper) instead.
        /// Create a Persistence object for the <see cref="T:System.Data.Entity.DbSet`1"/> to have data persisted or removed from
        /// Uses static API's Mapper for finding TypeMap between classes
        /// </summary>
        /// <typeparam name="TSource">Source table type to be updated</typeparam>
        /// <param name="source">Table to be updated</param>
        /// <returns>Persistence object to Update or Remove data</returns>
        [Obsolete("Use Persist(IMapper) instead.", true)]
        public static IPersistence Persist<TSource>(this Table<TSource> source)
            where TSource : class => throw new NotSupportedException();

        /// <summary>
        /// Create a Persistence object for the <see cref="T:System.Data.Entity.DbSet`1"/> to have data persisted or removed from
        /// </summary>
        /// <typeparam name="TSource">Source table type to be updated</typeparam>
        /// <param name="source">Table to be updated</param>
        /// <param name="mapper">IMapper used to find TypeMap between classes</param>
        /// <returns>Persistence object to Update or Remove data</returns>
        public static IPersistence Persist<TSource>(this Table<TSource> source, IMapper mapper)
            where TSource : class => new Persistence<TSource>(source, mapper);

        public static IEnumerable For<TSource>(this IQueryDataSourceInjection<TSource> source, Type destType)
        {
            var forMethod = source.GetType().GetMethod("For").MakeGenericMethod(destType);
            var listType = typeof(List<>).MakeGenericType(destType);
            var forResult = forMethod.Invoke(source, new object[] { null });
            return Activator.CreateInstance(listType, forResult) as IEnumerable;
        }
    }
}