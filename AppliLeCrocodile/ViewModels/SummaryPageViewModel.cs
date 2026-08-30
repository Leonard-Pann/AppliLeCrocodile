using AppliLeCrocodile.Configuration;
using AppliLeCrocodile.Services;

namespace AppliLeCrocodile.ViewModels;

public sealed class SummaryPageViewModel : MenuPageViewModel
{
    public SummaryPageViewModel(ILanguageManager languageManager, MenuLayoutOptions layoutOptions)
        : base(languageManager.GetText("SUMMARY_PAGE_ACCESSIBILITY"), layoutOptions)
    {
        BarName = languageManager.GetText("BAR_NAME");
        BarDescription = languageManager.GetText("BAR_DESCRIPTION");
        OpeningDays = languageManager.GetText("BAR_OPENING_DAYS");
        OpeningHours = languageManager.GetText("BAR_OPENING_HOURS");
        ClosedDays = languageManager.GetText("BAR_CLOSE_DAYS");
        HappyHourTitle = languageManager.GetText("HAPPY_HOUR");
        HappyHourDays = languageManager.GetText("HAPPY_HOURS_DAYS");
        HappyHourHours = languageManager.GetText("HAPPY_HOURS_HOURS");
        HappyHourCocktailPrice = languageManager.GetText("HAPPY_HOURS_COCKTAILS_PRICE");
        WeekCocktailPrice = languageManager.GetText("SUMMARY_COCKTAILS_PRICE_WEEK");
        And = languageManager.GetText("SUMMARY_AND");
        Weekend = languageManager.GetText("SUMMARY_WEEKEND");
        WeekendHappyHourHours = languageManager.GetText("WEEKEND_HAPPY_HOURS_HOURS");
        WeekendHappyHourPrice = languageManager.GetText("WEEKEND_HAPPY_HOURS_HOURS_PRICE");
        WeekendPrice = languageManager.GetText("WEEKEND_HAPPY_PRICE");
        Facebook = languageManager.GetText("SUMMARY_FACEBOOK");
        Address = languageManager.GetText("BAR_ADRESSE");
    }

    public string BarName { get; }

    public string BarDescription { get; }

    public string OpeningDays { get; }

    public string OpeningHours { get; }

    public string ClosedDays { get; }

    public string HappyHourTitle { get; }

    public string HappyHourDays { get; }

    public string HappyHourHours { get; }

    public string HappyHourCocktailPrice { get; }

    public string WeekCocktailPrice { get; }

    public string And { get; }

    public string Weekend { get; }

    public string WeekendHappyHourHours { get; }

    public string WeekendHappyHourPrice { get; }

    public string WeekendPrice { get; }

    public string Facebook { get; }

    public string Address { get; }
}
