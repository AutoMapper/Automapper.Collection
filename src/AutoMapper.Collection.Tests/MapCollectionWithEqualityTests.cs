using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper.EquivalencyExpression;
using FluentAssertions;
using Xunit;

namespace AutoMapper.Collection
{
    public class MapCollectionWithEqualityTests : MappingTestBase
    {
        protected virtual void ConfigureMapper(IMapperConfigurationExpression cfg)
        {
            cfg.AddCollectionMappers();
            cfg.CreateMap<ThingDto, Thing>().EqualityComparison((dto, entity) => dto.ID == entity.ID);
        }

        [Fact]
        public void Should_Keep_Existing_List()
        {
            var mapper = CreateMapper(ConfigureMapper);
            var dtos = new List<ThingDto>
            {
                new ThingDto { ID = 1, Title = "test0" },
                new ThingDto { ID = 2, Title = "test2" }
            };

            var items = new List<Thing>
            {
                new Thing { ID = 1, Title = "test1" },
                new Thing { ID = 3, Title = "test3" },
            };

            mapper.Map(dtos, items).Should().BeSameAs(items);
        }

        [Fact]
        public void Should_Update_Existing_Item()
        {
            var mapper = CreateMapper(ConfigureMapper);

            var dtos = new List<ThingDto>
            {
                new ThingDto { ID = 1, Title = "test0" },
                new ThingDto { ID = 2, Title = "test2" }
            };

            var items = new List<Thing>
            {
                new Thing { ID = 1, Title = "test1" },
                new Thing { ID = 3, Title = "test3" },
            };

            mapper.Map(dtos, items.ToList()).Should().HaveElementAt(0, items[0]);
        }

        [Fact]
        public void Should_Be_Fast_With_Large_Lists()
        {
            var mapper = CreateMapper(ConfigureMapper);

            var dtos = new object[100000].Select((_, i) => new ThingDto { ID = i }).ToList();

            var items = new object[100000].Select((_, i) => new Thing { ID = i }).ToList();

            mapper.Map(dtos, items.ToList()).Should().HaveElementAt(0, items[0]);
        }

        [Fact]
        public void Should_Be_Fast_With_Large_Reversed_Lists()
        {
            var mapper = CreateMapper(ConfigureMapper);

            var dtos = new object[100000].Select((_, i) => new ThingDto { ID = i }).ToList();
            dtos.Reverse();

            var items = new object[100000].Select((_, i) => new Thing { ID = i }).ToList();

            mapper.Map(dtos, items.ToList()).Should().HaveElementAt(0, items[0]);
        }

        [Fact]
        public void Should_Be_Fast_With_Large_Lists_MultiProperty_Mapping()
        {
            var mapper = CreateMapper(x =>
            {
                x.AddCollectionMappers();
                x.CreateMap<ThingDto, Thing>().EqualityComparison((ThingDto dto, Thing entity) => dto.ID == entity.ID && dto.ID == entity.ID);
            });

            var dtos = new object[100000].Select((_, i) => new ThingDto { ID = i }).ToList();

            var items = new object[100000].Select((_, i) => new Thing { ID = i }).ToList();

            mapper.Map(dtos, items.ToList()).Should().HaveElementAt(0, items[0]);
        }

        [Fact]
        public void Should_Be_Fast_With_Large_Lists_MultiProperty_Mapping_Cant_Extract()
        {
            var mapper = CreateMapper(x =>
            {
                x.AddCollectionMappers();
                x.CreateMap<ThingDto, Thing>().EqualityComparison((ThingDto dto, Thing entity) => dto.ID == entity.ID || dto.ID == entity.ID);
            });

            var dtos = new object[100000].Select((_, i) => new ThingDto { ID = i }).ToList();

            var items = new object[100000].Select((_, i) => new Thing { ID = i }).ToList();

            mapper.Map(dtos, items.ToList()).Should().HaveElementAt(0, items[0]);
        }

        [Fact]
        public void Should_Be_Fast_With_Large_Lists_Cant_Extract_Negative()
        {
            var mapper = CreateMapper(x =>
            {
                x.AddCollectionMappers();
                // ReSharper disable once NegativeEqualityExpression
                x.CreateMap<ThingDto, Thing>().EqualityComparison((ThingDto dto, Thing entity) => dto.ID == entity.ID);
            });

            var dtos = new object[100000].Select((_, i) => new ThingDto { ID = i }).ToList();

            var items = new object[100000].Select((_, i) => new Thing { ID = i }).ToList();

            mapper.Map(dtos, items.ToList()).Should().HaveElementAt(0, items[0]);
        }

        [Fact]
        public void Should_Be_Fast_With_Large_Lists_MultiProperty_Mapping_Cant_Extract_Negative()
        {
            var mapper = CreateMapper(x =>
            {
                x.AddCollectionMappers();
                // ReSharper disable once NegativeEqualityExpression
                x.CreateMap<ThingDto, Thing>().EqualityComparison((ThingDto dto, Thing entity) => dto.ID == entity.ID && dto.ID == entity.ID);
            });

            var dtos = new object[100000].Select((_, i) => new ThingDto { ID = i }).ToList();

            var items = new object[100000].Select((_, i) => new Thing { ID = i }).ToList();

            mapper.Map(dtos, items.ToList()).Should().HaveElementAt(0, items[0]);
        }

        [Fact]
        public void Should_Be_Fast_With_Large_Lists_SubObject()
        {
            var mapper = CreateMapper(x =>
            {
                x.AddCollectionMappers();
#pragma warning disable IDE0038 // can't use pattern matching as would get "Error CS8122 An expression tree may not contain an 'is' pattern - matching operator."
                x.CreateMap<ThingDto, Thing>().EqualityComparison((ThingDto source, Thing dest) => dest.ID == (source is ThingSubDto ? ((ThingSubDto)source).ID2 : source.ID));
#pragma warning restore IDE0038 // Use pattern matching
            });

            var dtos = new object[100000].Select((_, i) => new ThingSubDto { ID = i + 100000 }).Cast<ThingDto>().ToList();

            var items = new object[100000].Select((_, i) => new Thing { ID = i }).ToList();

            mapper.Map(dtos, items.ToList()).Should().HaveElementAt(0, items[0]);
        }

        [Fact]
        public void Should_Be_Fast_With_Large_Lists_SubObject_switch_left_and_right_expression()
        {
            var mapper = CreateMapper(x =>
            {
                x.AddCollectionMappers();
#pragma warning disable IDE0038 // can't use pattern matching as would get "Error CS8122 An expression tree may not contain an 'is' pattern - matching operator."
                x.CreateMap<ThingDto, Thing>().EqualityComparison((ThingDto source, Thing dest) => (source is ThingSubDto ? ((ThingSubDto)source).ID2 : source.ID) == dest.ID);
#pragma warning restore IDE0038 // Use pattern matching
            });

            var dtos = new object[100000].Select((_, i) => new ThingSubDto { ID = i + 100000 }).ToList();

            var items = new object[100000].Select((_, i) => new Thing { ID = i }).ToList();

            mapper.Map(dtos, items.ToList()).Should().HaveElementAt(0, items[0]);
        }

        [Fact]
        public void Should_Work_With_Conditionals()
        {
            var mapper = CreateMapper(cfg =>
            {
                cfg.AddCollectionMappers();
                cfg.CreateMap<ClientDto, Client>()
                    .ForMember(x => x.DtoId, m => m.Ignore())
                    .EqualityComparison((ClientDto src, Client dest) => dest.DtoId == 0 ? src.Code == dest.Code : src.Id == dest.DtoId);
            });

            var dto = new ClientDto
            {
                Code = "abc",
                Id = 1
            };
            var entity = new Client { Code = dto.Code, Id = 42 };
            var entityCollection = new List<Client> { entity };

            mapper.Map(new[] { dto }, entityCollection);

            entity.ShouldBeEquivalentTo(entityCollection[0]);
        }

        public class Client
        {
            public long Id { get; set; }
            public string Code { get; set; }
            public long DtoId { get; set; }
        }

        public class ClientDto
        {
            public long Id { get; set; }
            public string Code { get; set; }
        }

        [Fact]
        public void Should_Work_With_Null_Destination()
        {
            var mapper = CreateMapper(ConfigureMapper);

            var dtos = new List<ThingDto>
            {
                new ThingDto { ID = 1, Title = "test0" },
                new ThingDto { ID = 2, Title = "test2" }
            };

            mapper.Map<List<Thing>>(dtos).Should().HaveSameCount(dtos);
        }

        [Fact]
        public void Should_Work_With_Comparing_String_Types()
        {
            var mapper = CreateMapper(cfg =>
            {
                cfg.AddCollectionMappers();
                cfg.CreateMap<Charge, SaleCharge>()
                    .ForMember(d => d.SaleId, (IMemberConfigurationExpression<Charge, SaleCharge, Guid> o) => o.Ignore())
                    .EqualityComparison((Charge c, SaleCharge sc) => sc.Category == c.Category && sc.Description == c.Description);

                cfg.CreateMap<SaleCharge, Charge>()
                    .ConstructUsing(
                        saleCharge => new Charge(saleCharge.Category, saleCharge.Description, saleCharge.Value))
                    .EqualityComparison((SaleCharge sc, Charge c) => sc.Category == c.Category && sc.Description == c.Description);
            });

            var dto = new Charge("category", "description", 5);
            var entity = new SaleCharge { Category = dto.Category, Description = dto.Description };
            var entityCollection = new List<SaleCharge> { entity };

            mapper.Map(new[] { dto }, entityCollection);

            entity.ShouldBeEquivalentTo(entityCollection[0]);
        }

        public class Charge
        {
            public Charge(string category, string description, decimal value)
            {
                Category = category;
                Description = description;
                Value = value;
            }

            public string Category { get; }
            public string Description { get; }
            public decimal Value { get; }

            public override string ToString() => $"{Category}|{Description}|{Value}";

            public override int GetHashCode() => $"{Category}|{Description}|{Value}".GetHashCode();

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj)) return true;

                if (obj is null) return false;

                return obj is Charge chg && Category == chg.Category && Description == chg.Description && Value == chg.Value;
            }
        }

        public class SaleCharge
        {
            public Guid SaleId { get; set; }
            public string Category { get; set; }
            public string Description { get; set; }
            public decimal Value { get; set; }
        }

        [Fact]
        public void Should_Be_Instanced_Based()
        {
            var mapper = CreateMapper(x =>
            {
                x.AddCollectionMappers();
                x.CreateMap<ThingDto, Thing>().ReverseMap();
            });

            var dtos = new List<ThingDto>
            {
                new ThingDto { ID = 1, Title = "test0" },
                new ThingDto { ID = 2, Title = "test2" }
            };

            var items = new List<Thing>
            {
                new Thing { ID = 1, Title = "test1" },
                new Thing { ID = 3, Title = "test3" },
            };

            mapper.Map(dtos, items.ToList()).Should().NotContain(items[0]);
        }

        [Fact]
        public void Parent_Should_Be_Same_As_Root_Object()
        {
            var mapper = CreateMapper(
                    cfg =>
                    {
                        cfg.AddCollectionMappers();
                        cfg.CreateMap<ThingWithCollection, ThingWithCollection>()
                            .PreserveReferences();
                        cfg.CreateMap<ThingCollectionItem, ThingCollectionItem>()
                            .EqualityComparison((src, dst) => src.ID == dst.ID)
                            .PreserveReferences();
                    });

            var root = new ThingWithCollection()
            {
                Children = new List<ThingCollectionItem>()
            };
            root.Children.Add(new ThingCollectionItem() { ID = 1, Parent = root });

            var target = new ThingWithCollection() { Children = new List<ThingCollectionItem>() };
            mapper.Map(root, target).Should().Be(target);

            target.Children.Count.Should().Be(1);
            target.Children.Single().Parent.Should().Be(target);
        }

        public class Thing
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public override string ToString() => Title;
        }

        public class ThingSubDto : ThingDto
        {
            public int ID2 => ID - 100000;
        }

        public class ThingDto
        {
            public int ID { get; set; }
            public string Title { get; set; }

            public override string ToString() => Title;

        }

        public class ThingWithCollection
        {
            public ICollection<ThingCollectionItem> Children { get; set; }
        }

        public class ThingCollectionItem
        {
            public int ID { get; set; }
            public ThingWithCollection Parent { get; set; }
        }
    }
}