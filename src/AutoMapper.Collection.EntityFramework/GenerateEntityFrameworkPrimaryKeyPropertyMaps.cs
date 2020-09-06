using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;
using AutoMapper.EquivalencyExpression;

namespace AutoMapper.EntityFramework
{
    public class GenerateEntityFrameworkPrimaryKeyPropertyMaps<TDatabaseContext> : IGeneratePropertyMaps
        where TDatabaseContext : IObjectContextAdapter, new()
    {
        private readonly TDatabaseContext _context = new TDatabaseContext();
        private readonly MethodInfo _createObjectSetMethodInfo = typeof(ObjectContext).GetMethod("CreateObjectSet", Type.EmptyTypes);

        public IEnumerable<PropertyMap> GeneratePropertyMaps(TypeMap typeMap)
        {
            var propertyMaps = typeMap.PropertyMaps;
            try
            {
                var createObjectSetMethod = _createObjectSetMethodInfo.MakeGenericMethod(typeMap.DestinationType);
                dynamic objectSet = createObjectSetMethod.Invoke(_context.ObjectContext, null);

                IEnumerable<EdmMember> keyMembers = objectSet.EntitySet.ElementType.KeyMembers;
                return keyMembers.Select(m => propertyMaps.FirstOrDefault(p => p.DestinationMember.Name == m.Name));
            }
            catch (Exception)
            {
                return Enumerable.Empty<PropertyMap>();
            }
        }
    }
}