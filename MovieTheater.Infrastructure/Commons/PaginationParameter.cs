namespace MovieTheater.Infrastructure.Commons;

public class PaginationParameter
{
    private const int maxPageSize = 50;
    private int _pageSize = 5; // depend on use case
    public int PageIndex { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > maxPageSize ? maxPageSize : value;
    }
}