using LTU.SearchEngine.Backend.Core.Exceptions;
using LTU.SearchEngine.Backend.Core.RequestParameters;
using Microsoft.EntityFrameworkCore;

namespace LTU.SearchEngine.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IQueryable{T}"/> to facilitate asynchronous pagination.
/// </summary>
public static class QueryableExtensionPagination
{

	/// <summary>
	/// Executes a query asynchronously and wraps the results in a <see cref="PaginatedResult{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements in the source query.</typeparam>
	/// <param name="source">The <see cref="IQueryable{T}"/> to paginate.</param>
	/// <param name="paginationParameters">The parameters defining the requested page and size.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains a 
	/// <see cref="PaginatedResult{T}"/> including the subset of items and the calculated metadata.
	/// </returns>
	/// <remarks>
	/// This method performs two database operations:
	/// <list type="number">
	///     <item><description>A count operation to determine the total number of matching records.</description></item>
	///     <item><description>A filtered fetch using <c>Skip</c> and <c>Take</c> logic.</description></item>
	/// </list>
	/// </remarks>
	/// <exception cref="PaginationOutOfRangeException">
	/// Thrown if <paramref name="paginationParameters"/> contains a page number or page size less than 1.
	/// </exception>
	public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> source, 
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