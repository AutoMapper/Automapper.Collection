using AutoMapper.EquivalencyExpression;
using AutoMapper.Features;

namespace AutoMapper.Collection.Runtime
{
    public class CollectionMappingFeature : IRuntimeFeature
    {
        public CollectionMappingFeature(IEquivalentComparer equivalentComparer, bool useSourceOrder)
        {
            EquivalentComparer = equivalentComparer;
            UseSourceOrder = useSourceOrder;
        }

        public IEquivalentComparer EquivalentComparer { get; }

        public bool UseSourceOrder { get; }

        void IRuntimeFeature.Seal(IConfigurationProvider configurationProvider)
        {
        }
    }
}
