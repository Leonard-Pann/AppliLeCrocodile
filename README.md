AppliLeCrocodile est une application cross plateforme (Windows, Mac, Linux, Android et iOS) affichant la carte du bar Le Crocodile, situé dans le 5ᵉ arrondissement de Paris.

Vous y trouverez plus de 100 cocktails disponibles à la carte, ainsi que des fonctions de filtrage.

Développée en C# avec Avalonia pour Android, iOS et Desktop.

## Cocktail catalogue

The menu has five sections: cover, presentation, cocktails, soft drinks, and beers/shooters/snacks.
Swipe horizontally between sections and scroll vertically through the two-column cocktail catalogue.
Cocktails are alphabetical in row order, from left to right and then top to bottom.

Search matches cocktail names and ingredients, ignoring accents and case. Advanced filters combine with search:
selected alcohol levels are alternatives, while a cocktail must contain every selected ingredient.
Favorites and a minimum average rating can further restrict results. Rating sorting retains alphabetical order for ties.
Reset clears search and all filters without removing saved favorites.

`AppliLeCrocodile/Assets/Save/cocktails.json` now requires `alcoholLevel`: `1` (low), `2` (medium), or `3` (strong).
The initial values are random placeholders for manual correction. An optional `description` string supplies the detail text.
Photos are resolved under `Assets/Images/Cocktails/` using `imagePath`; missing photos show a placeholder.

Favorites are stored in `LeCrocodile/favorites.json` under the platform's local application data directory.
Writes are asynchronous and replace the file atomically. Invalid existing files are reported instead of overwritten.

`ICocktailReviewService` accepts cocktail identifiers and returns reviews with a user name, a 1–5 rating, and a comment.
`DemoCocktailReviewService` currently returns three repeatable sample reviews per cocktail, with empty comments.
Replace its registration in `CompositionRoot` with a server-backed implementation when the API is available.

## Regression checks

The dependency-free regression executable checks real menu data, ordering, combined filters, navigation,
reviews, favorite persistence, concurrent saves, and storage failures:

```powershell
dotnet run --project AppliLeCrocodile.Tests -c Release
```

<img width="2160" height="3027" alt="croco" src="https://github.com/user-attachments/assets/66dafd42-e3fd-469c-b614-47101ce7a918" />
