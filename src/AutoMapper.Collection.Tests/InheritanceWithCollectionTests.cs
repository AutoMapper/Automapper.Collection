using AutoMapper.EquivalencyExpression;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace AutoMapper.Collection
{
    public abstract class InheritanceWithCollectionTests : MappingTestBase
    {
        protected abstract void ConfigureMapper(IMapperConfigurationExpression cfg);

        [Fact]
        public void TypeMap_Should_include_base_types()
        {
            var mapper = CreateMapper(ConfigureMapper);
            var typeMap = mapper.ConfigurationProvider.ResolveTypeMap(typeof(MailOrderDomain), typeof(OrderEf));

            var typePairs = new[]{
                    new TypePair(typeof(OrderDomain), typeof(OrderEf))
            };
            typeMap.IncludedBaseTypes.ShouldBeEquivalentTo(typePairs);
        }

        [Fact]
        public void TypeMap_Should_include_derivied_types()
        {
            var mapper = CreateMapper(ConfigureMapper);
            var typeMap = mapper.ConfigurationProvider.ResolveTypeMap(typeof(OrderDomain), typeof(OrderEf));

            var typePairs = new[]{
                    new TypePair(typeof(OnlineOrderDomain), typeof(OnlineOrderEf)),
                    new TypePair(typeof(MailOrderDomain), typeof(MailOrderEf))
            };
            typeMap.IncludedDerivedTypes.ShouldBeEquivalentTo(typePairs);
        }

        [Fact]
        public void Map_Should_ReturnOnlineOrderEf_When_ListIsOfTypeOrderEf()
        {
            var mapper = CreateMapper(ConfigureMapper);

            //arrange
            var orderDomain = new OnlineOrderDomain { Id = "Id", Key = "Key" };
            var rootDomain = new RootDomain { OnlineOrders = { orderDomain } };

            //act
            RootEf mappedRootEf = mapper.Map<RootDomain, RootEf>(rootDomain);

            //assert
            OrderEf orderEf = mappedRootEf.Orders[0];

            orderEf.Should().BeOfType<OnlineOrderEf>();
            orderEf.Id.ShouldBeEquivalentTo(orderDomain.Id);

            var onlineOrderEf = (OnlineOrderEf)orderEf;
            onlineOrderEf.Key.ShouldBeEquivalentTo(orderDomain.Key);

            // ------------------------------------------------------------- //

            //arrange again
            mappedRootEf.Orders.Add(new OnlineOrderEf { Id = "NewId" });

            mapper.Map(mappedRootEf, rootDomain);

            //assert again
            rootDomain.OnlineOrders.Count.ShouldBeEquivalentTo(2);
            rootDomain.OnlineOrders.Last().Should().BeOfType<OnlineOrderDomain>();

            //Assert.AreSame(rootDomain.OnlineOrders.First(), orderDomain); that doesn't matter when we map from EF to Domain
        }

        [Fact]
        public void Map_FromEfToDomain_And_AddAnOnlineOrderInTheDomainObject_And_ThenMapBackToEf_Should_UseTheSameReferenceInTheEfCollection()
        {
            var mapper = CreateMapper(ConfigureMapper);

            //arrange
            var onlineOrderEf = new OnlineOrderEf { Id = "Id", Key = "Key" };
            var mailOrderEf = new MailOrderEf { Id = "MailOrderId" };
            var rootEf = new RootEf { Orders = { onlineOrderEf, mailOrderEf } };

            //act
            RootDomain mappedRootDomain = mapper.Map<RootEf, RootDomain>(rootEf);

            //assert
            OnlineOrderDomain onlineOrderDomain = mappedRootDomain.OnlineOrders[0];

            onlineOrderDomain.Should().BeOfType<OnlineOrderDomain>();
            onlineOrderEf.Id.ShouldBeEquivalentTo(onlineOrderEf.Id);

            // IMPORTANT ASSERT ------------------------------------------------------------- IMPORTANT ASSERT //

            //arrange again
            mappedRootDomain.OnlineOrders.Add(new OnlineOrderDomain { Id = "NewOnlineOrderId", Key = "NewKey" });
            mappedRootDomain.MailOrders.Add(new MailOrderDomain { Id = "NewMailOrderId", });
            onlineOrderDomain.Id = "Hi";

            //act again
            mapper.Map(mappedRootDomain, rootEf);

            //assert again
            OrderEf existingMailOrderEf = rootEf.Orders.Single(orderEf => orderEf.Id == mailOrderEf.Id);
            OrderEf existingOnlineOrderEf = rootEf.Orders.Single(orderEf => orderEf.Id == onlineOrderEf.Id);

            OrderEf newOnlineOrderEf = rootEf.Orders.Single(orderEf => orderEf.Id == "NewOnlineOrderId");
            OrderEf newMailOrderEf = rootEf.Orders.Single(orderEf => orderEf.Id == "NewMailOrderId");

            rootEf.Orders.Count.ShouldBeEquivalentTo(4);
            onlineOrderEf.Should().BeSameAs(existingOnlineOrderEf);
            mailOrderEf.Should().BeSameAs(existingMailOrderEf);

            newOnlineOrderEf.Should().BeOfType<OnlineOrderEf>();
            newMailOrderEf.Should().BeOfType<MailOrderEf>();
        }

        private static bool BaseEquals(OrderDomain oo, OrderEf dto) => oo.Id == dto.Id;

        private static bool DerivedEquals(OnlineOrderDomain ood, OnlineOrderEf ooe) => ood.Key == ooe.Key;

        public class MailOrderDomain : OrderDomain
        {
        }

        public class MailOrderEf : OrderEf
        {
        }

        public class OnlineOrderDomain : OrderDomain
        {
            public string Key { get; set; }
        }

        public class OnlineOrderEf : OrderEf
        {
            public string Key { get; set; }
        }

        public abstract class OrderDomain
        {
            public string Id { get; set; }
        }

        public abstract class OrderEf
        {
            public string Id { get; set; }
        }

        public class RootDomain
        {
            public List<OnlineOrderDomain> OnlineOrders { get; set; } = new List<OnlineOrderDomain>();
            public List<MailOrderDomain> MailOrders { get; set; } = new List<MailOrderDomain>();
        }

        public class RootEf
        {
            public List<OrderEf> Orders { get; set; } = new List<OrderEf>();
        }

        public class MergeDomainOrdersToEfOrdersValueResolver : IValueResolver<RootDomain, RootEf, List<OrderEf>>
        {
            public List<OrderEf> Resolve(RootDomain source, RootEf destination, List<OrderEf> destMember, ResolutionContext context)
            {
                var mappedOnlineOrders = new List<OrderEf>(destination.Orders);
                var mappedMailOrders = new List<OrderEf>(destination.Orders);

                context.Mapper.Map(source.OnlineOrders, mappedOnlineOrders);
                context.Mapper.Map(source.MailOrders, mappedMailOrders);

                return mappedOnlineOrders.Union(mappedMailOrders).ToList();
            }
        }

        public class Include : InheritanceWithCollectionTests
        {
            protected override void ConfigureMapper(IMapperConfigurationExpression cfg)
            {
                cfg.ShouldMapProperty = propertyInfo => propertyInfo.GetMethod.IsPublic || propertyInfo.GetMethod.IsAssembly || propertyInfo.GetMethod.IsFamily || propertyInfo.GetMethod.IsPrivate;
                cfg.AddCollectionMappers();

                //DOMAIN --> EF
                cfg.CreateMap<RootDomain, RootEf>()
                    .ForMember(rootEf => rootEf.Orders, opt => opt.MapFrom<MergeDomainOrdersToEfOrdersValueResolver>())
                    ;

                //collection type
                cfg.CreateMap<OrderDomain, OrderEf>()
                    .EqualityComparison((oo, dto) => BaseEquals(oo, dto))
                    .Include<MailOrderDomain, MailOrderEf>()
                    .Include<OnlineOrderDomain, OnlineOrderEf>()
                    ;

                cfg.CreateMap<OnlineOrderDomain, OnlineOrderEf>()
                    .EqualityComparison((ood, ooe) => DerivedEquals(ood, ooe))
                    ;

                cfg.CreateMap<MailOrderDomain, MailOrderEf>()
                    ;

                //EF --> DOMAIN
                cfg.CreateMap<RootEf, RootDomain>()
                    .ForMember(rootDomain => rootDomain.OnlineOrders, opt => opt.MapFrom(rootEf => rootEf.Orders.Where(orderEf => orderEf.GetType() == typeof(OnlineOrderEf))))
                    .ForMember(rootDomain => rootDomain.MailOrders, opt => opt.MapFrom(rootEf => rootEf.Orders.Where(orderEf => orderEf.GetType() == typeof(MailOrderEf))))
                    ;

                cfg.CreateMap<OrderEf, OrderDomain>()
                    .Include<OnlineOrderEf, OnlineOrderDomain>()
                    .Include<MailOrderEf, MailOrderDomain>()
                    ;

                cfg.CreateMap<OnlineOrderEf, OnlineOrderDomain>()
                    ;

                cfg.CreateMap<MailOrderEf, MailOrderDomain>()
                    ;
            }
        }

        public class IncludeBase : InheritanceWithCollectionTests
        {
            protected override void ConfigureMapper(IMapperConfigurationExpression cfg)
            {
                cfg.ShouldMapProperty = propertyInfo => propertyInfo.GetMethod.IsPublic || propertyInfo.GetMethod.IsAssembly || propertyInfo.GetMethod.IsFamily || propertyInfo.GetMethod.IsPrivate;
                cfg.AddCollectionMappers();

                //DOMAIN --> EF
                cfg.CreateMap<RootDomain, RootEf>()
                    .ForMember(rootEf => rootEf.Orders, opt => opt.MapFrom<MergeDomainOrdersToEfOrdersValueResolver>())
                    ;

                //collection type
                cfg.CreateMap<OrderDomain, OrderEf>()
                    .EqualityComparison((oo, dto) => BaseEquals(oo, dto))
                    ;

                cfg.CreateMap<OnlineOrderDomain, OnlineOrderEf>()
                    .EqualityComparison((ood, ooe) => DerivedEquals(ood, ooe))
                    .IncludeBase<OrderDomain, OrderEf>()
                    ;

                cfg.CreateMap<MailOrderDomain, MailOrderEf>()
                    .IncludeBase<OrderDomain, OrderEf>()
                    ;

                //EF --> DOMAIN
                cfg.CreateMap<RootEf, RootDomain>()
                    .ForMember(rootDomain => rootDomain.OnlineOrders, opt => opt.MapFrom(rootEf => rootEf.Orders.Where(orderEf => orderEf.GetType() == typeof(OnlineOrderEf))))
                    .ForMember(rootDomain => rootDomain.MailOrders, opt => opt.MapFrom(rootEf => rootEf.Orders.Where(orderEf => orderEf.GetType() == typeof(MailOrderEf))))
                    ;

                cfg.CreateMap<OrderEf, OrderDomain>()
                    ;

                cfg.CreateMap<OnlineOrderEf, OnlineOrderDomain>()
                    .IncludeBase<OrderEf, OrderDomain>()
                    ;

                cfg.CreateMap<MailOrderEf, MailOrderDomain>()
                    .IncludeBase<OrderEf, OrderDomain>()
                    ;
            }
        }
    }
}
