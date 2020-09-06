using System.Collections.Generic;
using AutoMapper.EquivalencyExpression;
using FluentAssertions;
using Xunit;

namespace AutoMapper.Collection
{
    public class NullableIdTests : MappingTestBase
    {
        [Fact]
        public void Should_Work_With_Null_Id()
        {
            var mapper = CreateMapper(x =>
            {
                x.AddCollectionMappers();
                x.CreateMap<ThingWithStringIdDto, ThingWithStringId>().EqualityComparison((dto, entity) => dto.ID == entity.ID);
            });

            var original = new List<ThingWithStringId>
            {
                new ThingWithStringId { ID = "1", Title = "test0" },
                new ThingWithStringId { ID = "2", Title = "test2" },
            };

            var dtos = new List<ThingWithStringIdDto>
            {
                new ThingWithStringIdDto { ID = "1", Title = "test0" },
                new ThingWithStringIdDto { ID = "2", Title = "test2" },
                new ThingWithStringIdDto { Title = "test3" }
            };

            mapper.Map(dtos, original);

            original.Should().HaveSameCount(dtos);
        }

        [Fact]
        public void Should_Work_With_Multiple_Null_Id()
        {
            var mapper = CreateMapper(x =>
            {
                x.AddCollectionMappers();
                x.CreateMap<ThingWithStringIdDto, ThingWithStringId>().EqualityComparison((dto, entity) => dto.ID == entity.ID);
            });

            var original = new List<ThingWithStringId>
            {
                new ThingWithStringId { ID = "1", Title = "test0" },
                new ThingWithStringId { ID = "2", Title = "test2" },
                new ThingWithStringId { ID = "3", Title = "test3" },
            };

            var dtos = new List<ThingWithStringIdDto>
            {
                new ThingWithStringIdDto { ID = "1", Title = "test0" },
                new ThingWithStringIdDto { ID = "2", Title = "test2" },
                new ThingWithStringIdDto { Title = "test3" },
                new ThingWithStringIdDto { Title = "test4" },
            };

            mapper.Map(dtos, original);

            original.Should().HaveSameCount(dtos);
        }

        public class ThingWithStringId
        {
            public string ID { get; set; }
            public string Title { get; set; }
            public override string ToString() => Title;
        }

        public class ThingWithStringIdDto
        {
            public string ID { get; set; }
            public string Title { get; set; }

            public override string ToString() => Title;
        }
    }
}
