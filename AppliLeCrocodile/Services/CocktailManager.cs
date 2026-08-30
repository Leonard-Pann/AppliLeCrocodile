using System.Globalization;
using AppliLeCrocodile.Infrastructure.Assets;
using AppliLeCrocodile.Infrastructure.Serialization;
using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services.Filters;

namespace AppliLeCrocodile.Services;

public sealed class CocktailManager : ICocktailManager
{
    private const string IngredientsPath = "Assets/Save/ingredients.json";
    private const string CocktailsPath = "Assets/Save/cocktails.json";
    private const string SoftsPath = "Assets/Save/softs.json";
    private const string FruitJuiceRecipesPath = "Assets/Save/fruit_juice.json";
    private const string BeersPath = "Assets/Save/beers.json";
    private const string SnacksPath = "Assets/Save/snacks.json";
    private const string SodasPath = "Assets/Save/soda.json";
    private const string ShootersPath = "Assets/Save/shooters.json";

    private readonly ILanguageManager languageManager;
    private readonly JsonResourceReader resourceReader;
    private readonly SemaphoreSlim loadingGate = new SemaphoreSlim(1, 1);
    private MenuCatalog? catalog;

    public CocktailManager(IAssetReader assetReader, ILanguageManager languageManager)
    {
        resourceReader = new JsonResourceReader(assetReader);
        this.languageManager = languageManager;
    }

    public async Task<MenuCatalog> LoadAsync(CancellationToken cancellationToken)
    {
        if (catalog is not null)
        {
            return catalog;
        }

        await loadingGate.WaitAsync(cancellationToken);
        try
        {
            if (catalog is not null)
            {
                return catalog;
            }

            Task<IngredientFileDto> ingredientsTask = resourceReader.ReadAsync(
                IngredientsPath,
                JsonSerialization.Context.IngredientFileDto,
                cancellationToken);
            Task<CocktailFileDto> cocktailsTask = resourceReader.ReadAsync(
                CocktailsPath,
                JsonSerialization.Context.CocktailFileDto,
                cancellationToken);
            Task<CocktailFileDto> softsTask = resourceReader.ReadAsync(
                SoftsPath,
                JsonSerialization.Context.CocktailFileDto,
                cancellationToken);
            Task<CocktailFileDto> fruitJuiceRecipesTask = resourceReader.ReadAsync(
                FruitJuiceRecipesPath,
                JsonSerialization.Context.CocktailFileDto,
                cancellationToken);
            Task<BeerFileDto> beersTask = resourceReader.ReadAsync(
                BeersPath,
                JsonSerialization.Context.BeerFileDto,
                cancellationToken);
            Task<SnackFileDto> snacksTask = resourceReader.ReadAsync(
                SnacksPath,
                JsonSerialization.Context.SnackFileDto,
                cancellationToken);
            Task<SodaFileDto> sodasTask = resourceReader.ReadAsync(
                SodasPath,
                JsonSerialization.Context.SodaFileDto,
                cancellationToken);
            Task<ShooterFileDto> shootersTask = resourceReader.ReadAsync(
                ShootersPath,
                JsonSerialization.Context.ShooterFileDto,
                cancellationToken);

            await Task.WhenAll(
                ingredientsTask,
                cocktailsTask,
                softsTask,
                fruitJuiceRecipesTask,
                beersTask,
                snacksTask,
                sodasTask,
                shootersTask);

            Ingredient[] ingredients = MapIngredients(await ingredientsTask);
            Cocktail[] cocktails = MapCocktails(await cocktailsTask);
            Cocktail[] softs = MapCocktails(await softsTask);
            Cocktail[] fruitJuiceRecipes = MapCocktails(await fruitJuiceRecipesTask);
            Beer[] beers = MapBeers(await beersTask);
            PricedMenuItem[] snacks = MapPricedItems((await snacksTask).Snacks);
            PricedMenuItem[] sodas = MapPricedItems((await sodasTask).Sodas);
            Shooter[] shooters = MapShooters(await shootersTask);

            ValidateMenuData(ingredients, cocktails, softs, fruitJuiceRecipes, beers, snacks, sodas, shooters);

            Array.Sort(cocktails, CompareCocktail);
            Array.Sort(softs, CompareSoft);
            Ingredient[] fruitJuices = ingredients.Where(ingredient => ingredient.IsFruitJuice).ToArray();
            Array.Sort(fruitJuices, CompareIngredient);

            catalog = new MenuCatalog(
                Array.AsReadOnly(cocktails),
                Array.AsReadOnly(softs),
                Array.AsReadOnly(fruitJuiceRecipes),
                Array.AsReadOnly(ingredients),
                Array.AsReadOnly(fruitJuices),
                Array.AsReadOnly(beers),
                Array.AsReadOnly(snacks),
                Array.AsReadOnly(sodas),
                Array.AsReadOnly(shooters));
            return catalog;
        }
        finally
        {
            loadingGate.Release();
        }
    }

    public Ingredient GetIngredientById(string nameId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameId);
        Ingredient? ingredient = GetCatalog().Ingredients.FirstOrDefault(
            candidate => StringComparer.Ordinal.Equals(candidate.NameId, nameId));
        return ingredient ?? throw new KeyNotFoundException($"Ingredient identifier '{nameId}' does not exist.");
    }

    public IReadOnlyList<Cocktail> GetCocktails(ICocktailFilter? filter)
    {
        return FilterCocktails(GetCatalog().Cocktails, filter);
    }

    public IReadOnlyList<Cocktail> GetSofts(ICocktailFilter? filter)
    {
        return FilterCocktails(GetCatalog().Softs, filter);
    }

    public IReadOnlyList<Ingredient> GetFruitJuices()
    {
        return GetCatalog().FruitJuices.ToArray();
    }

    public IReadOnlyList<Beer> GetBeers()
    {
        return GetCatalog().Beers.ToArray();
    }

    public IReadOnlyList<PricedMenuItem> GetSnacks()
    {
        return GetCatalog().Snacks.ToArray();
    }

    public IReadOnlyList<PricedMenuItem> GetSodas()
    {
        return GetCatalog().Sodas.ToArray();
    }

    public IReadOnlyList<Shooter> GetShooters()
    {
        return GetCatalog().Shooters.ToArray();
    }

    private int CompareCocktail(Cocktail right, Cocktail left)
    {
        return CompareLocalizedText(right.NameId, left.NameId);
    }

    private int CompareIngredient(Ingredient right, Ingredient left)
    {
        return CompareLocalizedText(right.NameId, left.NameId);
    }

    private int CompareSoft(Cocktail right, Cocktail left)
    {
        if (StringComparer.Ordinal.Equals(right.NameId, "MESANGE") && StringComparer.Ordinal.Equals(left.NameId, "MARABOUT"))
        {
            return -1;
        }

        if (StringComparer.Ordinal.Equals(right.NameId, "MARABOUT") && StringComparer.Ordinal.Equals(left.NameId, "MESANGE"))
        {
            return 1;
        }

        return CompareCocktail(right, left);
    }

    private int CompareLocalizedText(string rightTextId, string leftTextId)
    {
        string rightText = languageManager.GetText(rightTextId);
        string leftText = languageManager.GetText(leftTextId);
        string cultureName = StringComparer.Ordinal.Equals(languageManager.CurrentLanguage, LanguageManager.DefaultLanguage)
            ? "fr-FR"
            : "en-US";
        CompareInfo compareInfo = CultureInfo.GetCultureInfo(cultureName).CompareInfo;
        return compareInfo.Compare(rightText, leftText, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.StringSort);
    }

    private void ValidateMenuData(
        IReadOnlyList<Ingredient> ingredients,
        IReadOnlyList<Cocktail> cocktails,
        IReadOnlyList<Cocktail> softs,
        IReadOnlyList<Cocktail> fruitJuiceRecipes,
        IReadOnlyList<Beer> beers,
        IReadOnlyList<PricedMenuItem> snacks,
        IReadOnlyList<PricedMenuItem> sodas,
        IReadOnlyList<Shooter> shooters)
    {
        List<string> errors = [];
        ValidateUniqueIdentifiers(ingredients.Select(ingredient => ingredient.NameId), IngredientsPath, errors);
        ValidateUniqueIdentifiers(cocktails.Select(cocktail => cocktail.NameId), CocktailsPath, errors);
        ValidateUniqueIdentifiers(softs.Select(soft => soft.NameId), SoftsPath, errors);
        ValidateUniqueIdentifiers(fruitJuiceRecipes.Select(recipe => recipe.NameId), FruitJuiceRecipesPath, errors);
        ValidateUniqueIdentifiers(beers.Select(beer => beer.NameId), BeersPath, errors);
        ValidateUniqueIdentifiers(snacks.Select(snack => snack.NameId), SnacksPath, errors);
        ValidateUniqueIdentifiers(sodas.Select(soda => soda.NameId), SodasPath, errors);
        ValidateUniqueIdentifiers(shooters.Select(shooter => shooter.NameId), ShootersPath, errors);

        HashSet<string> ingredientIds = ingredients.Select(ingredient => ingredient.NameId).ToHashSet(StringComparer.Ordinal);
        ValidateCocktails(cocktails, CocktailsPath, ingredientIds, errors);
        ValidateCocktails(softs, SoftsPath, ingredientIds, errors);
        ValidateCocktails(fruitJuiceRecipes, FruitJuiceRecipesPath, ingredientIds, errors);

        foreach (Beer beer in beers)
        {
            ValidateTextId(beer.NameId, BeersPath, errors);
            ValidateTextId(beer.HalfPintPriceId, BeersPath, errors);
            ValidateTextId(beer.PintPriceId, BeersPath, errors);
        }

        foreach (PricedMenuItem item in snacks.Concat(sodas))
        {
            ValidateTextId(item.NameId, "priced menu data", errors);
            ValidateTextId(item.PriceId, "priced menu data", errors);
        }

        foreach (Shooter shooter in shooters)
        {
            ValidateTextId(shooter.NameId, ShootersPath, errors);
        }

        if (errors.Count > 0)
        {
            throw new InvalidDataException("Menu validation failed:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }
    }

    private void ValidateCocktails(
        IEnumerable<Cocktail> cocktails,
        string sourcePath,
        IReadOnlySet<string> ingredientIds,
        ICollection<string> errors)
    {
        foreach (Cocktail cocktail in cocktails)
        {
            ValidateTextId(cocktail.NameId, sourcePath, errors);

            if (cocktail.IngredientNameIds.Count == 0)
            {
                errors.Add($"- Cocktail '{cocktail.NameId}' in '{sourcePath}' has no ingredients.");
            }

            foreach (string ingredientId in cocktail.IngredientNameIds)
            {
                if (!ingredientIds.Contains(ingredientId))
                {
                    errors.Add($"- Cocktail '{cocktail.NameId}' references unknown ingredient '{ingredientId}'.");
                }

                ValidateTextId(ingredientId, sourcePath, errors);
            }
        }
    }

    private void ValidateTextId(string textId, string source, ICollection<string> errors)
    {
        if (!languageManager.TryGetText(textId, out string _))
        {
            errors.Add($"- Text identifier '{textId}' referenced by '{source}' does not exist.");
        }
    }

    private static void ValidateUniqueIdentifiers(IEnumerable<string> identifiers, string sourcePath, ICollection<string> errors)
    {
        HashSet<string> uniqueIdentifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (string identifier in identifiers)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                errors.Add($"- Asset '{sourcePath}' contains an empty identifier.");
            }
            else if (!uniqueIdentifiers.Add(identifier))
            {
                errors.Add($"- Asset '{sourcePath}' contains duplicate identifier '{identifier}'.");
            }
        }
    }

    private static Ingredient[] MapIngredients(IngredientFileDto file)
    {
        return file.Ingredients.Select(ingredient => new Ingredient(
            ingredient.NameId,
            ingredient.DescriptionId,
            ingredient.AlcoholVolume,
            ingredient.IsLiquid,
            ingredient.IsFruitJuice,
            ingredient.ImagePath)).ToArray();
    }

    private static Cocktail[] MapCocktails(CocktailFileDto file)
    {
        return file.Cocktails.Select(cocktail => new Cocktail(
            cocktail.NameId,
            cocktail.ImagePath,
            Array.AsReadOnly(cocktail.IngredientNameIds.ToArray()))).ToArray();
    }

    private static Beer[] MapBeers(BeerFileDto file)
    {
        return file.Beers.Select(beer => new Beer(
            beer.NameId,
            beer.ImagePath,
            beer.DescriptionId,
            beer.HalfPintPriceId,
            beer.PintPriceId)).ToArray();
    }

    private static PricedMenuItem[] MapPricedItems(IEnumerable<PricedMenuItemDto> items)
    {
        return items.Select(item => new PricedMenuItem(item.NameId, item.PriceId)).ToArray();
    }

    private static Shooter[] MapShooters(ShooterFileDto file)
    {
        return file.Shooters.Select(shooter => new Shooter(shooter.NameId)).ToArray();
    }

    private static IReadOnlyList<Cocktail> FilterCocktails(IEnumerable<Cocktail> source, ICocktailFilter? filter)
    {
        Cocktail[] results = filter is null
            ? source.ToArray()
            : source.Where(filter.IsValidCocktail).ToArray();
        return Array.AsReadOnly(results);
    }

    private MenuCatalog GetCatalog()
    {
        return catalog ?? throw new InvalidOperationException("CocktailManager must be loaded before accessing menu data.");
    }
}
