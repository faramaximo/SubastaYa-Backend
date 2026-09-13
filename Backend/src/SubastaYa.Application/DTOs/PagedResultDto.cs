namespace SubastaYa.Application.DTOs;

public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int TotalItems { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;

    public PagedResultDto() { }

    public PagedResultDto(IEnumerable<T> items, int totalItems, int page, int pageSize)
    {
        Items = items;
        TotalItems = totalItems;
        Page = page;
        PageSize = pageSize;
    }
}
