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

`CompositionRoot` registers `HttpCocktailReviewService`. Reviews are retrieved from the HTTPS API when a cocktail detail
opens, when the user refreshes it, and after a successful submission. Comments load in pages of 20; the displayed average
and the rating filters always use the aggregate of all reviews. The catalogue fetches lightweight summaries at startup.
`DemoCocktailReviewService` remains available for tests and explicit demonstrations only.

Set `baseUrl` in `AppliLeCrocodile/Assets/Configuration/reviews-api.json` before building the application.
The default `https://localhost:7443/` is for Desktop development on the server computer. On a phone, localhost means
the phone itself: use the server's reachable DNS name and a certificate trusted by Android/iOS. Certificate validation
is enabled and redirects are refused. No cleartext or certificate-validation bypass is included.

The backend and its Windows setup guide are in the sibling `../AppliLeCrocodileServer` directory.
Keep both directories beside one another: the application references its `LeCrocodile.Reviews.Contracts` project,
which targets .NET 10 for compatibility with the mobile projects. The API itself targets .NET 11.

The detail view opens a comment form with a nickname, a 1–5 star selector, and a comment.
`IReviewerNameStore` persists only the nickname in `LeCrocodile/reviewer-name.json` under local application data.
It is requested on the first submission and reused across cocktails and application restarts, without authentication or accounts.
Review text and ratings are not stored locally; failed submissions keep the draft open for retry.
`IInstallationIdentity` atomically creates a random 128-bit UUID in `LeCrocodile/installation-id.txt` on the initial
review-service initialization and reuses it on every request. It is an anonymous installation identifier, not authentication.
Sending another review for the same cocktail updates that installation's previous rating and comment.
The server returns HTTP 429 with `Retry-After` when an IP exceeds five accepted requests in a rolling second.
The client spaces its own requests and performs one bounded retry for HTTP 429/503. Offline failures preserve the menu.

## Regression checks

The dependency-free regression executable checks real menu data, ordering, combined filters, navigation,
reviews, favorite persistence, concurrent saves, and storage failures:

```powershell
dotnet run --project AppliLeCrocodile.Tests -c Release
```

<img width="2160" height="3027" alt="croco" src="https://github.com/user-attachments/assets/66dafd42-e3fd-469c-b614-47101ce7a918" />
