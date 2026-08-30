using AppliLeCrocodile.Configuration;

namespace AppliLeCrocodile.ViewModels;

public sealed class CocktailMenuPageViewModel : MenuPageViewModel
{
    public CocktailMenuPageViewModel(
        string accessibilityName,
        int menuPageNumber,
        IReadOnlyList<CocktailItemViewModel> leftColumn,
        IReadOnlyList<CocktailItemViewModel> rightColumn,
        MenuLayoutOptions layoutOptions)
        : base($"{accessibilityName} {menuPageNumber}", layoutOptions)
    {
        LeftColumn = leftColumn;
        RightColumn = rightColumn;
        IsRegularPage = true;
        NoResultsText = string.Empty;
    }

    private CocktailMenuPageViewModel(
        string accessibilityName,
        IReadOnlyList<CocktailItemViewModel> leftColumn,
        IReadOnlyList<CocktailItemViewModel> rightColumn,
        string noResultsText,
        MenuLayoutOptions layoutOptions)
        : base(accessibilityName, layoutOptions)
    {
        LeftColumn = leftColumn;
        RightColumn = rightColumn;
        NoResultsText = noResultsText;
        HasNoSearchResults = leftColumn.Count == 0;
        IsSingleColumnSearch = leftColumn.Count > 0 && rightColumn.Count == 0;
        IsTwoColumnSearch = rightColumn.Count > 0;
    }

    public IReadOnlyList<CocktailItemViewModel> LeftColumn { get; }

    public IReadOnlyList<CocktailItemViewModel> RightColumn { get; }

    public bool IsRegularPage { get; }

    public bool IsSingleColumnSearch { get; }

    public bool IsTwoColumnSearch { get; }

    public bool HasNoSearchResults { get; }

    public string NoResultsText { get; }

    public static CocktailMenuPageViewModel CreateSearchResults(
        string accessibilityName,
        IReadOnlyList<CocktailItemViewModel> leftColumn,
        IReadOnlyList<CocktailItemViewModel> rightColumn,
        string noResultsText,
        MenuLayoutOptions layoutOptions)
    {
        return new CocktailMenuPageViewModel(
            accessibilityName,
            leftColumn,
            rightColumn,
            noResultsText,
            layoutOptions);
    }
}
