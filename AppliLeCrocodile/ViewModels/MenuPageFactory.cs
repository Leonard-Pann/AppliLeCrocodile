using AppliLeCrocodile.Configuration;
using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services;
using AppliLeCrocodile.Services.Layout;

namespace AppliLeCrocodile.ViewModels;

public sealed class MenuPageFactory
{
    private readonly ILanguageManager languageManager;
    private readonly MenuLayoutOptions layoutOptions;
    private readonly ICocktailItemLayoutCalculator cocktailItemLayoutCalculator;

    public MenuPageFactory(
        ILanguageManager languageManager,
        MenuLayoutOptions layoutOptions,
        ICocktailItemLayoutCalculator cocktailItemLayoutCalculator)
    {
        this.languageManager = languageManager;
        this.layoutOptions = layoutOptions;
        this.cocktailItemLayoutCalculator = cocktailItemLayoutCalculator;
    }

    public IReadOnlyList<MenuPageViewModel> CreatePages(MenuCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        List<MenuPageViewModel> pages =
        [
            new CoverPageViewModel(languageManager.GetText("COVER_PAGE_ACCESSIBILITY"), layoutOptions),
            new SummaryPageViewModel(languageManager, layoutOptions)
        ];

        CocktailItemViewModel[] cocktailItems = catalog.Cocktails.Select(CreateCocktailItem).ToArray();
        IReadOnlyList<CocktailPageColumns> cocktailPages = PartitionCocktails(cocktailItems);
        for (int pageIndex = 0; pageIndex < cocktailPages.Count; pageIndex++)
        {
            CocktailPageColumns columns = cocktailPages[pageIndex];
            pages.Add(new CocktailMenuPageViewModel(
                languageManager.GetText("COCKTAILS_PAGE_ACCESSIBILITY"),
                pageIndex + 1,
                columns.LeftColumn,
                columns.RightColumn,
                layoutOptions));
        }

        pages.Add(CreateSoftPage(catalog));
        pages.Add(CreateLastPage(catalog));
        return pages.AsReadOnly();
    }

    private IReadOnlyList<CocktailPageColumns> PartitionCocktails(IReadOnlyList<CocktailItemViewModel> cocktails)
    {
        List<CocktailPageColumns> pages = [];
        int cocktailIndex = 0;

        while (cocktailIndex < cocktails.Count)
        {
            IReadOnlyList<CocktailItemViewModel> leftColumn = FillColumn(cocktails, ref cocktailIndex);
            IReadOnlyList<CocktailItemViewModel> rightColumn = FillColumn(cocktails, ref cocktailIndex);
            pages.Add(new CocktailPageColumns(leftColumn, rightColumn));
        }

        return pages.AsReadOnly();
    }

    public CocktailMenuPageViewModel CreateSearchResultsPage(IReadOnlyList<Cocktail> cocktails)
    {
        CocktailItemViewModel[] items = cocktails.Select(CreateCocktailItem).ToArray();
        int leftCount = GetSearchResultLeftColumnCount(items);
        CocktailItemViewModel[] leftColumn = items.Take(leftCount).ToArray();
        CocktailItemViewModel[] rightColumn = items.Skip(leftCount).ToArray();

        return CocktailMenuPageViewModel.CreateSearchResults(
            languageManager.GetText("SEARCH_RESULTS_ACCESSIBILITY"),
            Array.AsReadOnly(leftColumn),
            Array.AsReadOnly(rightColumn),
            languageManager.GetText("NO_COCKTAIL"),
            layoutOptions);
    }

    private IReadOnlyList<CocktailItemViewModel> FillColumn(
        IReadOnlyList<CocktailItemViewModel> cocktails,
        ref int cocktailIndex)
    {
        List<CocktailItemViewModel> column = [];
        double usedHeight = 0d;

        while (cocktailIndex < cocktails.Count)
        {
            CocktailItemViewModel cocktail = cocktails[cocktailIndex];
            double itemHeight = MeasureHeight(cocktail);
            if (itemHeight > cocktailItemLayoutCalculator.AvailableColumnHeight)
            {
                string message = $"Cocktail '{cocktail.Name}' is taller than an empty cocktail column "
                    + "and cannot be displayed without clipping.";
                throw new InvalidDataException(message);
            }

            if (column.Count > 0 && usedHeight + itemHeight > cocktailItemLayoutCalculator.AvailableColumnHeight)
            {
                break;
            }

            column.Add(cocktail);
            usedHeight += itemHeight;
            cocktailIndex++;
        }

        return column.AsReadOnly();
    }

    private int GetSearchResultLeftColumnCount(IReadOnlyList<CocktailItemViewModel> cocktails)
    {
        if (cocktails.Count == 0)
        {
            return 0;
        }

        double totalHeight = cocktails.Sum(MeasureHeight);
        if (totalHeight <= cocktailItemLayoutCalculator.AvailableColumnHeight)
        {
            return cocktails.Count;
        }

        double targetHeight = totalHeight / 2d;
        double leftHeight = 0d;
        int leftCount = 0;

        while (leftCount < cocktails.Count - 1 && leftHeight < targetHeight)
        {
            leftHeight += MeasureHeight(cocktails[leftCount]);
            leftCount++;
        }

        return leftCount;
    }

    private double MeasureHeight(CocktailItemViewModel cocktail)
    {
        return cocktailItemLayoutCalculator.MeasureHeight(cocktail.Name, cocktail.Ingredients);
    }

    private SoftPageViewModel CreateSoftPage(MenuCatalog catalog)
    {
        int leftCount = (catalog.Softs.Count + 1) / 2;
        CocktailItemViewModel[] leftColumn = catalog.Softs.Take(leftCount).Select(CreateCocktailItem).ToArray();
        CocktailItemViewModel[] rightColumn = catalog.Softs.Skip(leftCount).Select(CreateCocktailItem).ToArray();
        string fruitJuices = string.Join(", ", catalog.FruitJuices.Select(juice => languageManager.GetText(juice.NameId)));

        return new SoftPageViewModel(
            languageManager.GetText("SOFT_PAGE_ACCESSIBILITY"),
            languageManager.GetText("SOFT_TITLE"),
            Array.AsReadOnly(leftColumn),
            Array.AsReadOnly(rightColumn),
            languageManager.GetText("FRUIT_JUICE_TITLE"),
            languageManager.GetText("CHOOSE"),
            fruitJuices,
            layoutOptions);
    }

    private LastPageViewModel CreateLastPage(MenuCatalog catalog)
    {
        BeerItemViewModel[] beers = catalog.Beers.Select(beer => new BeerItemViewModel(
            languageManager.GetText(beer.NameId),
            languageManager.GetText(beer.HalfPintPriceId),
            languageManager.GetText(beer.PintPriceId))).ToArray();
        PricedItemViewModel[] snacks = catalog.Snacks.Select(CreatePricedItem).ToArray();
        PricedItemViewModel[] sodas = catalog.Sodas.Select(CreatePricedItem).ToArray();
        int leftSodaCount = (sodas.Length + 1) / 2;
        IReadOnlyList<IReadOnlyList<string>> shooterColumns = CreateShooterColumns(catalog.Shooters);

        return new LastPageViewModel(
            languageManager.GetText("LAST_PAGE_ACCESSIBILITY"),
            languageManager.GetText("BEER_TITLE"),
            Array.AsReadOnly(beers),
            languageManager.GetText("SNACKS_TITLE"),
            Array.AsReadOnly(snacks),
            languageManager.GetText("SODA_TITLE"),
            Array.AsReadOnly(sodas.Take(leftSodaCount).ToArray()),
            Array.AsReadOnly(sodas.Skip(leftSodaCount).ToArray()),
            languageManager.GetText("SHOOTERS_TITLE"),
            Array.AsReadOnly(new[]
            {
                languageManager.GetText("ONE_SHOOTER_PRICE"),
                languageManager.GetText("FIVE_SHOOTER_PRICE"),
                languageManager.GetText("TEN_SHOOTER_PRICE")
            }),
            shooterColumns,
            languageManager.GetText("BUY_MENU_CARD"),
            layoutOptions);
    }

    private IReadOnlyList<IReadOnlyList<string>> CreateShooterColumns(IReadOnlyList<Shooter> shooters)
    {
        int itemsPerColumn = (int)Math.Ceiling(shooters.Count / (double)layoutOptions.ShooterColumnCount);
        List<IReadOnlyList<string>> columns = new List<IReadOnlyList<string>>(layoutOptions.ShooterColumnCount);

        for (int columnIndex = 0; columnIndex < layoutOptions.ShooterColumnCount; columnIndex++)
        {
            string[] names = shooters
                .Skip(columnIndex * itemsPerColumn)
                .Take(itemsPerColumn)
                .Select(shooter => languageManager.GetText(shooter.NameId))
                .ToArray();
            columns.Add(Array.AsReadOnly(names));
        }

        return columns.AsReadOnly();
    }

    private CocktailItemViewModel CreateCocktailItem(Cocktail cocktail)
    {
        string ingredients = string.Join(", ", cocktail.IngredientNameIds.Select(languageManager.GetText));
        return new CocktailItemViewModel(languageManager.GetText(cocktail.NameId), ingredients);
    }

    private PricedItemViewModel CreatePricedItem(PricedMenuItem item)
    {
        return new PricedItemViewModel(languageManager.GetText(item.NameId), languageManager.GetText(item.PriceId));
    }

    private sealed record CocktailPageColumns(
        IReadOnlyList<CocktailItemViewModel> LeftColumn,
        IReadOnlyList<CocktailItemViewModel> RightColumn);
}
