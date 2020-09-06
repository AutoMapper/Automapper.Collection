using System;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;

namespace AutoMapper.Collection.LinqToSQL
{
    public class Persistence<TTo> : IPersistence
        where TTo : class
    {
        private readonly Table<TTo> _sourceSet;
        private readonly IMapper _mapper;

        public Persistence(Table<TTo> sourceSet, IMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sourceSet = sourceSet;
        }

        public void InsertOrUpdate<TFrom>(TFrom from)
            where TFrom : class => InsertOrUpdate(typeof(TFrom), from);

        public void InsertOrUpdate(Type type, object from)
        {
            if (!(_mapper.Map(from, type, typeof(Expression<Func<TTo, bool>>)) is Expression<Func<TTo, bool>> equivExpr))
                return;

            var to = _sourceSet.FirstOrDefault(equivExpr);

            if (to == null)
            {
                to = Activator.CreateInstance<TTo>();
                _sourceSet.InsertOnSubmit(to);
            }
            _mapper.Map(from, to);
        }

        public void Remove<TFrom>(TFrom from)
            where TFrom : class
        {
            var equivExpr = _mapper.Map<TFrom, Expression<Func<TTo, bool>>>(from);
            if (equivExpr == null) return;
            var to = _sourceSet.FirstOrDefault(equivExpr);

            if (to != null) _sourceSet.DeleteOnSubmit(to);
        }
    }
}