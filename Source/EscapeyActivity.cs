// SPDX-License-Identifier: MPL-2.0
#if ANDROID
namespace Escapey;

using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using static Android.Content.PM.ConfigChanges;

/// <summary>The activity for creating and maintaining <see cref="EscapeyGame"/>.</summary>
[Activity(
     ConfigurationChanges = Orientation | Keyboard | KeyboardHidden | ScreenSize,
     AlwaysRetainTaskState = true,
     Icon = "@drawable/icon",
     Label = "@string/app_name",
     LaunchMode = LaunchMode.SingleInstance,
     MainLauncher = true,
     ScreenOrientation = ScreenOrientation.FullUser
 ), CLSCompliant(false)]
public sealed class EscapeyActivity : AndroidGameActivity
{
    /// <summary>The <see cref="EscapeyGame"/> instance.</summary>
    EscapeyGame? _game;

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        base.OnDestroy();
        _game?.Dispose();
        _game = null;
    }

    /// <inheritdoc />
    protected override void OnCreate(Bundle? bundle)
    {
        base.OnCreate(bundle);
        _game?.Dispose();
        (_game = new()).Run();
        SetContentView(_game.Services.GetService<View>());
    }
}

[CLSCompliant(false)]
partial class Resource;
#endif
