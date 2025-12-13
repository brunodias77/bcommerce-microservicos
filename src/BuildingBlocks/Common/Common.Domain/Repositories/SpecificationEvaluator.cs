using Common.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Common.Domain.Repositories;

/// <summary>
/// Avaliador de especificações para Entity Framework
/// </summary>
public static class SpecificationEvaluator<T> where T : Entity
{
    public static IQueryable<T> GetQuery(
        IQueryable<T> inputQuery,
        ISpecification<T> specification)
    {
        var query = inputQuery;

        // Apply criteria
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Include navigation properties
        query = specification.Includes
            .Aggregate(query, (current, include) => current.Include(include));

        // Include navigation properties as strings
        query = specification.IncludeStrings
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply grouping
        if (specification.GroupBy != null)
        {
            query = query.GroupBy(specification.GroupBy).SelectMany(x => x);
        }

        // Apply paging
        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }
}