using AppliLeCrocodile.Configuration;
using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services;

namespace AppliLeCrocodile.ViewModels;

public sealed class MenuPageFactory
{
    private readonly ILanguageManager languageManager;
    private readonly MenuLayoutOptions layoutOptions;

    public MenuPageFactory(ILanguageManager languageManager, MenuLayoutOptions layoutOptions)
    {
        this.languageManager = languageManager;
        this.layoutOptions = layoutOptions;
    }

    public IReadOnlyList<MenuPageViewModel> CreatePages(MenuCatalog catalog, CocktailMenuPageViewModel cocktailPage)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return Array.AsReadOnly<MenuPageViewModel>(
        [
            new CoverPageViewModel(languageManager.GetText("COVER_PAGE_ACCESSIBILITY"), layoutOptions),
            new SummaryPageViewModel(languageManager, layoutOptions),
            cocktailPage,
            CreateSoftPage(catalog),
            CreateLastPage(catalog)
        ]);
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
}
