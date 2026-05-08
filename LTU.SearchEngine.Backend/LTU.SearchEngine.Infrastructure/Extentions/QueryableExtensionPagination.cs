using LTU.SearchEngine.Backend.Core.Exceptions;
using LTU.SearchEngine.Backend.Core.RequestParameters;
using Microsoft.EntityFrameworkCore;

namespace LTU.SearchEngine.Infrastructure.Extensions;

public static class QueryableExtensionPagination
{
    public static async Task<IPaginatedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> source, 
        // int pageNumber, 
        // int pageSize
        PaginationRequestParameters paginationParameters
        )
    {
        var (pageNumber, pageSize) = paginationParameters;

        if (pageNumber < 1) throw new PaginationOutOfRangeException(nameof(pageNumber), "Page number must be higher than 0.");
        if (pageSize < 1) throw new PaginationOutOfRangeException(nameof(pageSize), "Page size must be higher than 0.");

        int count = await source.CountAsync();

        var items = await source
            .Skip(pageSize * (pageNumber - 1))
            .Take(pageSize)
            .ToListAsync();

        var MetaData = new PaginationMetaData(
            currentPage: pageNumber,
            totalPages: (int)Math.Ceiling(count / (double)pageSize),
            pageSize: pageSize,
            totalItemCount: count
        );    

        return new PaginatedResult<T>(items, MetaData);
    }
}