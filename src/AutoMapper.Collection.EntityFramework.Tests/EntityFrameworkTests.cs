using System;
using System.Data.Entity;
using System.Linq;
using AutoMapper.EntityFramework;
using AutoMapper.EquivalencyExpression;
using FluentAssertions;
using Xunit;

namespace AutoMapper.Collection.EntityFramework.Tests
{
    public class EntityFrameworkTests : MappingTestBase, IDisposable
    {
        private readonly IMapper mapper;
        private readonly DB db;             // IDisposable

        public EntityFrameworkTests()
        {
            mapper = CreateMapper(ConfigureMapper);
            db = new DB();
            // could hoist rest of Assert into ctor but I choose not to (some of my later tests will have different seeding)
            /*
                db.Things.Add(new Thing { Title = "Test2" });
                db.Things.Add(new Thing { Title = "Test3" });
                db.Things.Add(new Thing { Title = "Test4" });
                db.SaveChanges();
            */
        }

        private void ConfigureMapper(IMapperConfigurationExpression cfg)
        {
            cfg.AddCollectionMappers();
            cfg.CreateMap<ThingDto, Thing>().ReverseMap();
            cfg.SetGeneratePropertyMaps<GenerateEntityFrameworkPrimaryKeyPropertyMaps<DB>>();
        }

        [Fact]
        public void Should_Persist_To_Update()
        {
            const string newtitle = "Test";

            // Arrange (mapper and db initialised in ctor)
            db.Things.Add(new Thing { Title = "Test2" });
            db.Things.Add(new Thing { Title = "Test3" });
            db.Things.Add(new Thing { Title = "Test4" });
            db.SaveChanges();

            Assert.Equal(3, db.Things.Count());

            var item = db.Things.First();

            // Act.1
            db.Things.Persist(mapper).InsertOrUpdate(new ThingDto { ID = item.ID, Title = newtitle });

            // Assert.1
            Assert.Equal(1, db.ChangeTracker.Entries<Thing>().Count(x => x.State == EntityState.Modified));
            Assert.Equal(3, db.Things.Count());

            // Act.2
            db.SaveChanges();

            // Assert.2
            Assert.Equal(3, db.Things.Count());
            db.Things.First(x => x.ID == item.ID).Title.Should().Be(newtitle);
        }

        [Fact]
        public void Should_Persist_To_Insert()
        {
            const string newtitle = "Test";

            // Arrange (mapper and db initialised in ctor)
            db.Things.Add(new Thing { Title = "Test2" });
            db.Things.Add(new Thing { Title = "Test3" });
            db.Things.Add(new Thing { Title = "Test4" });
            db.SaveChanges();

            Assert.Equal(3, db.Things.Count());

            // Act.1
            db.Things.Persist(mapper).InsertOrUpdate(new ThingDto { Title = newtitle });

            // Assert.1
            Assert.Equal(3, db.Things.Count());
            Assert.Equal(1, db.ChangeTracker.Entries<Thing>().Count(x => x.State == EntityState.Added));

            // Act.2
            db.SaveChanges();

            // Assert.2
            Assert.Equal(4, db.Things.Count());

            db.Things.OrderByDescending(x => x.ID).First().Title.Should().Be(newtitle);
        }

        [Fact]
        /// <summary>
        ///     EF recognizes setting property to the same value so does not raise a pointless UPDATE statement
        /// </summary>
        /// <remarks>this behaviour is like INPC in GUI land</remarks>
        public void Should_Persist_Same_To_Unchanged()
        {
            // Arrange (mapper and db initialised in ctor)
            db.Things.Add(new Thing { Title = "Test2" });
            db.Things.Add(new Thing { Title = "Test3" });
            db.Things.Add(new Thing { Title = "Test4" });
            db.SaveChanges();

            Assert.Equal(3, db.Things.Count());

            var item = db.Things.First();

            // Act.1
            db.Things.Persist(mapper).InsertOrUpdate(new ThingDto { ID = item.ID, Title = item.Title });

            // Assert.1
            Assert.Equal(3, db.ChangeTracker.Entries<Thing>().Count(x => x.State == EntityState.Unchanged));

            // Act.2
            db.SaveChanges();

            // Assert.2
            Assert.Equal(3, db.Things.Count());
            db.Things.First(x => x.ID == item.ID).Title.Should().Be(item.Title);
        }

        [Fact]
        public void Should_Persist_Exist_To_Delete()
        {
            const string ignoredtitle = "Test";

            // Arrange (mapper and db initialised in ctor)
            db.Things.Add(new Thing { Title = "Test2" });
            db.Things.Add(new Thing { Title = "Test3" });
            db.Things.Add(new Thing { Title = "Test4" });
            db.SaveChanges();

            var item = db.Things.First();

            Assert.Equal(3, db.Things.Count());

            // Act.1
            db.Things.Persist(mapper).Remove(new ThingDto { ID = item.ID, Title = ignoredtitle });

            // Assert.1
            Assert.Equal(3, db.Things.Count());
            Assert.Equal(1, db.ChangeTracker.Entries<Thing>().Count(x => x.State == EntityState.Deleted));

            // Act.2
            db.SaveChanges();

            // Assert.2
            Assert.Equal(2, db.Things.Count());
            db.Things.FirstOrDefault(x => x.ID == item.ID).Should().BeNull();
        }

        /// <summary>
        ///     Remove on non-existent DELETEs is successful (i.e. silent and does not throw exception)
        /// </summary>
        /// <remarks>
        /// 1.  i.e. our silent behaviour differs from the bleat from
        ///      ADO    DeletedRowInaccessibleException
        ///      Linq   ChangeConflictException
        ///      EF     DbUpdateException / DbUpdateConcurrencyException
        /// 2.  this test exists to ensure current behaviour is not usurped by any future breaking change!
        /// </remarks>
        [Fact]
        public void Should_Persist_NotExist_To_Delete()
        {
            const string ignoredtitle = "Test";

            // Arrange (mapper and db initialised in ctor)
            db.Things.Add(new Thing { Title = "Test2" });
            db.Things.Add(new Thing { Title = "Test3" });
            db.Things.Add(new Thing { Title = "Test4" });
            db.SaveChanges();

            Assert.Equal(3, db.Things.Count());

            // Act.1
            db.Things.Persist(mapper).Remove(new ThingDto { Title = ignoredtitle });    // NB ID=0 means unsaved record

            // Assert.1
            Assert.Equal(3, db.Things.Count());
            Assert.Equal(0, db.ChangeTracker.Entries<Thing>().Count(x => x.State != EntityState.Unchanged));

            // Act.2
            db.SaveChanges();

            // Assert.2
            Assert.Equal(3, db.Things.Count());
            db.Things.FirstOrDefault(x => x.Title == ignoredtitle).Should().BeNull();
        }

        public void Dispose() => db?.Dispose();

        public class DB : DbContext
        {
            public DB()
                : base(Effort.DbConnectionFactory.CreateTransient(), contextOwnsConnection: true)
            {
                Things.RemoveRange(Things);
                SaveChanges();
            }

            public DbSet<Thing> Things { get; set; }
        }

        public class Thing
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public override string ToString() => Title;
        }

        public class ThingDto
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public override string ToString() => Title;
        }
    }
}
