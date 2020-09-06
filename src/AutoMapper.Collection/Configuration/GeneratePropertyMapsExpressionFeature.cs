using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper.Collection.Runtime;
using AutoMapper.EquivalencyExpression;
using AutoMapper.Features;

namespace AutoMapper.Collection.Configuration
{
    public class GeneratePropertyMapsExpressionFeature : IGlobalFeature
    {
        private readonly List<Func<Func<Type, object>, IGeneratePropertyMaps>> _generators = new List<Func<Func<Type, object>, IGeneratePropertyMaps>>();

        public void Add(Func<Func<Type, object>, IGeneratePropertyMaps> creator) => _generators.Add(creator);

        void IGlobalFeature.Configure(IConfigurationProvider configurationProvider)
        {
            var generators = _generators
                .Select(x => x.Invoke(configurationProvider.ServiceCtor))
                .ToList();
            configurationProvider.Features.Set(new GeneratePropertyMapsFeature(generators));
        }
    }
}
