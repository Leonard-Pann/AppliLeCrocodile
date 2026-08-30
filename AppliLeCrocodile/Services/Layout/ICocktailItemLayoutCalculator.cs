namespace AppliLeCrocodile.Services.Layout;

public interface ICocktailItemLayoutCalculator
{
    double AvailableColumnHeight { get; }

    double MeasureHeight(string name, string ingredients);
}
