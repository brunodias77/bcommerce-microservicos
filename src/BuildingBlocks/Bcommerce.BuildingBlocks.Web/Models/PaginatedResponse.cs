namespace Bcommerce.BuildingBlocks.Web.Models;

public class PaginatedResponse<T>
{
    public IEnumerable<T> Items { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public long TotalCount { get; }
    public int TotalPages { get; }
    public bool HasNextPage => PageIndex < TotalPages;
    public bool HasPreviousPage => PageIndex > 1;

    public PaginatedResponse(IEnumerable<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = count;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        Items = items;
    }
}
