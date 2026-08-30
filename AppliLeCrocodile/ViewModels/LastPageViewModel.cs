using AppliLeCrocodile.Configuration;

namespace AppliLeCrocodile.ViewModels;

public sealed class LastPageViewModel : MenuPageViewModel
{
    public LastPageViewModel(
        string accessibilityName,
        string beerTitle,
        IReadOnlyList<BeerItemViewModel> beers,
        string snackTitle,
        IReadOnlyList<PricedItemViewModel> snacks,
        string sodaTitle,
        IReadOnlyList<PricedItemViewModel> leftSodas,
        IReadOnlyList<PricedItemViewModel> rightSodas,
        string shooterTitle,
        IReadOnlyList<string> shooterPrices,
        IReadOnlyList<IReadOnlyList<string>> shooterColumns,
        string closingText,
        MenuLayoutOptions layoutOptions)
        : base(accessibilityName, layoutOptions)
    {
        BeerTitle = beerTitle;
        Beers = beers;
        SnackTitle = snackTitle;
        Snacks = snacks;
        SodaTitle = sodaTitle;
        LeftSodas = leftSodas;
        RightSodas = rightSodas;
        ShooterTitle = shooterTitle;
        ShooterPrices = shooterPrices;
        ShooterLeftColumn = shooterColumns.Count > 0 ? shooterColumns[0] : [];
        ShooterCenterColumn = shooterColumns.Count > 1 ? shooterColumns[1] : [];
        ShooterRightColumn = shooterColumns.Count > 2 ? shooterColumns[2] : [];
        ClosingText = closingText;
    }

    public string BeerTitle { get; }

    public IReadOnlyList<BeerItemViewModel> Beers { get; }

    public string SnackTitle { get; }

    public IReadOnlyList<PricedItemViewModel> Snacks { get; }

    public string SodaTitle { get; }

    public IReadOnlyList<PricedItemViewModel> LeftSodas { get; }

    public IReadOnlyList<PricedItemViewModel> RightSodas { get; }

    public string ShooterTitle { get; }

    public IReadOnlyList<string> ShooterPrices { get; }

    public IReadOnlyList<string> ShooterLeftColumn { get; }

    public IReadOnlyList<string> ShooterCenterColumn { get; }

    public IReadOnlyList<string> ShooterRightColumn { get; }

    public string ClosingText { get; }
}

public sealed class BeerItemViewModel
{
    public BeerItemViewModel(string name, string halfPintPrice, string pintPrice)
    {
        Name = name;
        HalfPintPrice = halfPintPrice;
        PintPrice = pintPrice;
    }

    public string Name { get; }

    public string HalfPintPrice { get; }

    public string PintPrice { get; }
}

public sealed class PricedItemViewModel
{
    public PricedItemViewModel(string name, string price)
    {
        Name = name;
        Price = price;
    }

    public string Name { get; }

    public string Price { get; }
}
