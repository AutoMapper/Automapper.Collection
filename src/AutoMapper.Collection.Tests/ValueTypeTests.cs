using System.Collections.Generic;
using System.Linq;
using AutoMapper.EquivalencyExpression;
using Xunit;

namespace AutoMapper.Collection
{
    public class ValueTypeTests
    {
        [Fact]
        public void MapValueTypes()
        {
            var mapper = new Mapper(new MapperConfiguration(c =>
            {
                c.AddCollectionMappers();

                c.CreateMap<Country, CountryDto>()
                    .ForMember(x => x.Nationalities, m => m.MapFrom(x => x.Persons))
                    .ReverseMap();

                c.CreateMap<int, PersonNationality>()
                    .EqualityComparison((src, dest) => dest.NationalityCountryId == src);
            }));

            var persons = new[]
            {
                new PersonNationality{PersonId = 1, NationalityCountryId = 101},
                new PersonNationality{PersonId = 2, NationalityCountryId = 102},
                new PersonNationality{PersonId = 3, NationalityCountryId = 103},
                new PersonNationality{PersonId = 4, NationalityCountryId = 104},
            };

            var country = new Country { Persons = new List<PersonNationality>(persons) };
            var countryDto = new CountryDto { Nationalities = new List<int> { 104, 103, 105 } };

            mapper.Map(countryDto, country);

            Assert.NotStrictEqual(new[] { persons[3], persons[2], country.Persons.Last() }, country.Persons);
            Assert.Equal(0, country.Persons.Last().PersonId);
        }

        public class PersonNationality
        {
            public int PersonId { get; set; }
            public int NationalityCountryId { get; set; }
        }

        public class Country
        {
            public IList<PersonNationality> Persons { get; set; }
        }

        public class CountryDto
        {
            public IList<int> Nationalities { get; set; }
        }
    }
}
