// SPDX-License-Identifier: MPL-2.0
#if ANDROID
namespace Escapey;

using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Android.Content.PM;

/// <summary>The activity for creating and maintaining <see cref="EscapeyGame"/>.</summary>
[Activity(
    ConfigurationChanges = ConfigChanges.Orientation |
        ConfigChanges.Keyboard |
        ConfigChanges.KeyboardHidden |
        ConfigChanges.ScreenSize,
    AlwaysRetainTaskState = true,
    Icon = "@drawable/icon",
    Label = "@string/app_name",
    LaunchMode = LaunchMode.SingleInstance,
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.FullUser
)]
public sealed class EscapeyActivity : AndroidGameActivity
{
    /// <summary>The <see cref="EscapeyGame"/> instance.</summary>
    EscapeyGame? _game;

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        base.OnDestroy();
        DisposeOf(ref _game);
    }

    /// <inheritdoc />
    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);
        DisposeOf(ref _game);
        _game = new();
        _game.Run();
        SetContentView(_game.Services.GetService<View>());
    }
}
#endif
