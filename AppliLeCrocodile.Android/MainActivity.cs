using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace AppliLeCrocodile.Android;

[Activity(
    Label = "AppliLeCrocodile.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    WindowSoftInputMode = global::Android.Views.SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
