namespace ImportOps.Application.Common;
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}