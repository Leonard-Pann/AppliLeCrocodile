using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppliLeCrocodile.ViewModels;

public sealed partial class IngredientChoiceViewModel : ViewModelBase
{
    private readonly Action selectionChanged;

    [ObservableProperty]
    private bool isSelected;

    public IngredientChoiceViewModel(string nameId, string name, Action selectionChanged)
    {
        NameId = nameId;
        Name = name;
        this.selectionChanged = selectionChanged;
    }

    public string NameId { get; }

    public string Name { get; }

    [RelayCommand]
    private void Toggle()
    {
        IsSelected = !IsSelected;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        selectionChanged();
    }
}
