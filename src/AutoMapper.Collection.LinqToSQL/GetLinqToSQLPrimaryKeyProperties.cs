using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using AutoMapper.EquivalencyExpression;

namespace AutoMapper.Collection.LinqToSQL
{
    public class GetLinqToSQLPrimaryKeyProperties : IGeneratePropertyMaps
    {
        public IEnumerable<PropertyMap> GeneratePropertyMaps(TypeMap typeMap)
        {
            var propertyMaps = typeMap.PropertyMaps;

            return typeMap.DestinationType.GetProperties()
                .Where(IsPrimaryKey)
                .Select(m => propertyMaps.FirstOrDefault(p => p.DestinationMember.Name == m.Name))
                .ToList();
        }

        private static bool IsPrimaryKey(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes(typeof(ColumnAttribute), false)
                .OfType<ColumnAttribute>().Any(ca => ca.IsPrimaryKey);
        }
    }
}