using System.Globalization;

namespace EasySave.GUI.Models;

public sealed class PaginationItem
{
    private PaginationItem()
    {
    }

    public int PageNumber { get; init; }
    public string Label { get; init; } = string.Empty;
    public bool IsEllipsis { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsPage => !IsEllipsis;
    public bool IsSelectable => !IsEllipsis && !IsCurrent;

    public static PaginationItem Page(int pageNumber, bool isCurrent)
    {
        return new PaginationItem
        {
            PageNumber = pageNumber,
            Label = pageNumber.ToString(CultureInfo.InvariantCulture),
            IsCurrent = isCurrent
        };
    }

    public static PaginationItem Ellipsis()
    {
        return new PaginationItem
        {
            Label = "...",
            IsEllipsis = true
        };
    }
}
