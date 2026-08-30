using AppliLeCrocodile.Configuration;

namespace AppliLeCrocodile.ViewModels;

public sealed class SoftPageViewModel : MenuPageViewModel
{
    public SoftPageViewModel(
        string accessibilityName,
        string softTitle,
        IReadOnlyList<CocktailItemViewModel> leftColumn,
        IReadOnlyList<CocktailItemViewModel> rightColumn,
        string fruitJuiceTitle,
        string chooseText,
        string fruitJuices,
        MenuLayoutOptions layoutOptions)
        : base(accessibilityName, layoutOptions)
    {
        SoftTitle = softTitle;
        LeftColumn = leftColumn;
        RightColumn = rightColumn;
        FruitJuiceTitle = fruitJuiceTitle;
        ChooseText = chooseText;
        FruitJuices = fruitJuices;
    }

    public string SoftTitle { get; }

    public IReadOnlyList<CocktailItemViewModel> LeftColumn { get; }

    public IReadOnlyList<CocktailItemViewModel> RightColumn { get; }

    public string FruitJuiceTitle { get; }

    public string ChooseText { get; }

    public string FruitJuices { get; }
}
