using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper.EquivalencyExpression;

namespace AutoMapper.Collection
{
    public class MapCollectionWithEqualityThreadSafetyTests
    {
        public async Task Should_Work_When_Initialized_Concurrently()
        {
            void act() => new MapperConfiguration(cfg => cfg.AddCollectionMappers());
            var tasks = new List<Task>();
            for (var i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(act));
            }

            await Task.WhenAll(tasks.ToArray());
        }
    }
}
