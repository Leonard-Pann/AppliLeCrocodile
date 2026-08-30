using AppliLeCrocodile.Configuration;

namespace AppliLeCrocodile.ViewModels;

public abstract class MenuPageViewModel : ViewModelBase
{
    protected MenuPageViewModel(string accessibilityName, MenuLayoutOptions layoutOptions)
    {
        AccessibilityName = accessibilityName;
        ReferenceWidth = layoutOptions.ReferenceWidth;
        ReferenceHeight = layoutOptions.ReferenceHeight;
    }

    public string AccessibilityName { get; }

    public int ReferenceWidth { get; }

    public int ReferenceHeight { get; }
}
