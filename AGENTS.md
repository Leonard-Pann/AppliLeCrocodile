# Project Instructions

## Scope

These instructions apply to the entire solution and all of its projects.

## Project Context

- This is a cross-platform Avalonia application for Android and iOS, with Desktop.
- The application displays the food and drink menu of a bar named Le Crocodile.
- Menu content is presented page by page, with navigation inspired by reading a book.
- Keep shared UI, navigation, models, services, and view models in the main `AppliLeCrododile` project whenever possible.
- Keep platform projects limited to startup code and genuinely platform-specific integrations.

## Language

- Write all code in English and in C#.
- Use English for identifiers, file names, comments, XML documentation, exceptions, logs, and developer-facing messages.
- User-facing menu content may use the language required by the product or its localization resources.

## C# Conventions

- Never use the `var` keyword. Always write the explicit type, including local variables, `foreach` variables, and `using` declarations.
- Keep nullable reference types enabled and handle nullability explicitly.
- Prefer simple, readable, idiomatic C# over clever or overly abstract code.
- Use `PascalCase` for types, methods, properties, events, and public members.
- Use `camelCase` for parameters, local variables and private instance fields.
- Prefix interfaces with `I`.
- Use meaningful names and avoid abbreviations unless they are standard and unambiguous.
- Prefer `readonly` fields and immutable data when mutation is not required.
- Use `async` and `await` for asynchronous work, and suffix asynchronous methods with `Async`.
- Pass a `CancellationToken` through asynchronous operations when cancellation is useful.
- Prefer guard clauses to deeply nested conditionals.
- Dispose of owned disposable resources correctly with `using` statements or declarations.

## Formatting

- Use four spaces for indentation and do not use tabs.
- Keep lines at or below 130 characters whenever practical.
- Keep any complete instruction, declaration, expression, method signature, argument list, or parameter list on one line when it is shorter than 130 characters.
- Keep method names and all parameters on one line when the complete signature is shorter than 130 characters.
- Keep method invocations and all arguments on one line when the complete invocation is shorter than 130 characters.
- Keep ternary expressions on one line when the complete expression is shorter than 130 characters.
- Only wrap a construct when its complete line would reach or exceed 130 characters, or when wrapping is required for clarity.
- Use braces for control-flow blocks, even when the body contains a single statement.
- Place opening braces on a new line for namespaces, types, methods, properties, and control-flow blocks.
- Keep one declaration or executable statement per line.

## JSON

- Use only the JSON serializer included with the .NET framework: `System.Text.Json.JsonSerializer`.
- Do not add or use Newtonsoft.Json or another third-party JSON serializer.
- Centralize shared `JsonSerializerOptions` instead of recreating equivalent options at each call site.
- Use explicit DTOs and strongly typed serialization or deserialization whenever possible.
- Use `JsonPropertyName` only when the external JSON contract differs from the English C# naming conventions.

## Avalonia and Architecture

- Follow MVVM and keep business logic out of code-behind files.
- Use CommunityToolkit.Mvvm for observable properties and commands where it reduces boilerplate clearly.
- Keep views focused on presentation and bindings; keep application state and behavior in view models and services.
- Use compiled bindings with `x:DataType` where supported.
- Avoid service-locator access from views and view models; prefer constructor injection.
- Keep navigation testable and independent from platform-specific UI APIs.
- Do not perform file, network, or JSON work on the UI thread.
- Update UI-bound state on the UI thread when required by Avalonia.
- Put reusable styles, colors, dimensions, and control templates in shared resource dictionaries.
- Prefer responsive layouts that work on different Android and iOS screen sizes and orientations.
- Respect safe areas, touch-friendly target sizes, and mobile accessibility.
- Provide design-time data when it materially improves XAML development without affecting runtime behavior.

## Models and Menu Data

- Keep domain models independent from Avalonia controls and view types.
- Represent cocktails, drinks, categories, prices, ingredients, and menu pages with strongly typed models.
- Keep display order explicit and deterministic.
- Treat menu data as read-only after loading unless an editing feature explicitly requires mutation.
- Validate deserialized menu data and report actionable errors instead of silently ignoring invalid content.

## Quality

- Preserve existing behavior unless the requested change explicitly modifies it.
- Add or update tests for non-trivial view-model, navigation, serialization, and menu-ordering behavior.
- Build every affected project after changes when the available environment supports its target platform.
- Do not introduce a new dependency when the framework or an existing project dependency already provides the required capability.
