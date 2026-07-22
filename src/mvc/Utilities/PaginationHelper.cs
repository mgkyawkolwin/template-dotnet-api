namespace Template.Api.Utilities;

public static class PaginationHelper
{
    public static int NormalizePage(int page) => page > 0 ? page : 1;

    public static int NormalizePageSize(int pageSize) => pageSize > 0 ? pageSize : 20;

    public static int CalculateTotalPages(int total, int pageSize) => total <= 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
}