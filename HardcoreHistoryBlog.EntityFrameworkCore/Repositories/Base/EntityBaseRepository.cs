﻿using AutoMapper;
using HardcoreHistory.Client.Interfaces;
using HardcoreHistory.Client.Shared.Common;
using HardcoreHistory.Client.Shared.Interfaces;
using HardcoreHistory.Infrastructure.Context;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardcoreHistory.Infrastructure.Repositories.Base
{
    public class EntityBaseRepository<T> where T : class
    {
        protected ApplicationDbContext _context;
        protected IMapper _mapper;

        public EntityBaseRepository(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public void Add(T item)
        {
            _context.Set<T>().Attach(item);
        }

        public async Task<T> GetAsync(int id)
        {
            var data = await _context.Set<T>()
                .FindAsync(id);

            return _mapper.Map<T>(data);
        }

        public async Task<List<T>> QueryAsync(IQuerySpecification<T> specification)
        {
            return (await PagedQueryAsync(specification)).Items;
        }

        public async Task<PagedQueryResult<T>> PagedQueryAsync(IQuerySpecification<T> specification)
        {
            var query = _context.Set<T>().AsQueryable();

            if (specification.Includes != null)
                specification.Includes.ForEach(x => { query = query.Include(x); });

            if (specification.StringIncludes != null)
                specification.StringIncludes.ForEach(x => { query = query.Include(x); });

            query = query.WhereIf(specification.WhereClause != null, specification.WhereClause);

            int totalCount = 0;

            if (specification.PerformCount)
                totalCount = await query.CountAsync();

            if (specification.OrderBy != null)
                query = specification.SortOrder == SortOrder.Ascending ? query.OrderBy(specification.OrderBy) : query.OrderByDescending(specification.OrderBy);

            if (specification.Take.HasValue && specification.Skip.HasValue)
                query = query.Skip(specification.Skip.Value).Take(specification.Take.Value);

            if (specification.DisableTracking)
                query = query.AsNoTracking();

            var items = _mapper.Map<List<T>>(await query.ToListAsync());

            return new PagedQueryResult<T>(items, totalCount);
        }
    }
}
