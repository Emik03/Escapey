// SPDX-License-Identifier: MPL-2.0
namespace Escapey;

using static OperatingSystem;

/// <summary>The application for drawing the adorable character Escapey.</summary>
[CLSCompliant(false)]
public sealed partial class EscapeyGame : Game
{
    /// <summary>The set of animations.</summary>
    readonly Animations _animations;

    /// <summary>The file system watcher for the config.</summary>
    readonly FileSystemWatcher _watcher;

    /// <summary>The model for speech recognition.</summary>
    readonly HearMonitor _hearMonitor;

    /// <summary>The config.</summary>
    Config _config;

    /// <summary>The mouth to use when the user is not speaking.</summary>
    Sprite.Mouth _neutral = Sprite.Mouth.Happy;

    /// <summary>Manages toggle states.</summary>
    Toggle _rainbow, _visible;

    /// <summary>Initializes a new instance of the <see cref="EscapeyGame"/> class.</summary>
    public EscapeyGame()
    {
        const int Width = 930, Height = 779;

        new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = Width, PreferredBackBufferHeight = Height, SynchronizeWithVerticalRetrace = true,
        }.ApplyChanges();

        _animations = new Animations(this, Width, Height)
           .Add<Sprite.Legs>()
           .Add<Sprite.Body>()
           .Add(Sprite.Eyes.Happy)
           .Add(Sprite.Mouth.Happy, 3)
           .Add<Sprite.Keys.Background>()
           .Add<Sprite.Keys.First>(visible: false)
           .Add<Sprite.Keys.Second>(visible: false)
           .Add<Sprite.Keys.Third>(visible: false)
           .Add<Sprite.Keys.Fourth>(visible: false)
           .Add<Sprite.Keys.Overlay>()
           .Add<Sprite.Arm.Right>()
           .Add<Sprite.Arm.Left>()
           .Sync<Sprite.Legs, Sprite.Arm.Left>();

        _watcher = new(Path.GetDirectoryName(Config.TextFile).OrEmpty(), Path.GetFileName(Config.TextFile))
        {
            EnableRaisingEvents = true,
        };

        LoadConfig();
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        _hearMonitor = HearMonitor.From(this, _config);
        GraphicsDevice.BlendState = BlendState.NonPremultiplied;
        _watcher.Changed += LoadConfig; // Re-read only after HearMonitor loads; causes undefined behavior otherwise.
    }

    /// <summary>Runs the game.</summary>
    public static void Go()
    {
        using EscapeyGame game = new();
        game.Run();
    }

    /// <inheritdoc />
    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            _hearMonitor.Dispose();
            _watcher.Dispose();
            _config.Dispose();
        }

        base.Dispose(isDisposing);
    }

    /// <inheritdoc />
    protected override void Draw(GameTime gameTime)
    {
        var columns = _config.Input.Poll().InvertIf(_config.Inverted);
        var sound = _hearMonitor.Poll();
        var pushed = columns.Has(Columns.Hide);
        var toggled = _visible.Accept(!pushed);
        var brightness = (byte)(_config.RainbowBrightness * byte.MaxValue);
        var saturation = (byte)(_config.RainbowSaturation * byte.MaxValue);
        var time = (int)(gameTime.TotalGameTime.Ticks * _config.RainbowSpeed / TimeSpan.TicksPerMillisecond);
        var color = _rainbow.Accept(columns.Has(Columns.Rainbow)) ? time.ToColor(saturation, brightness) : Color.White;

        _animations
           .Background(_config.Background)
           .Change(columns.ToEyes())
           .Change(sound.IsSpeaking() ? sound : columns.ToMouth(ref _neutral))
           .Change(toggled ? columns.ToLeftArm() : Sprite.Arm.Left.Idle)
           .Change(toggled ? columns.ToRightArm() : Sprite.Arm.Right.Idle)
           .Colored<Sprite.Eyes>(color)
           .Colored<Sprite.Mouth>(color)
           .SetVisibility<Sprite.Keys.Overlay>(toggled)
           .SetVisibility<Sprite.Keys.Background>(toggled)
           .SetVisibility<Sprite.Keys.First>(toggled && columns.Has(Columns.First))
           .SetVisibility<Sprite.Keys.Second>(toggled && columns.Has(Columns.Second))
           .SetVisibility<Sprite.Keys.Third>(toggled && columns.Has(Columns.Third))
           .SetVisibility<Sprite.Keys.Fourth>(toggled && columns.Has(Columns.Fourth))
           .Draw(gameTime);

        _hearMonitor.Draw(gameTime);
    }

    /// <inheritdoc />
    protected override void Update(GameTime gameTime) { }

    /// <summary>Loads or reloads the config.</summary>
    /// <param name="_">The sender, ignored.</param>
    /// <param name="__">The event args, ignored.</param>
    [MemberNotNull(nameof(_config))]
    void LoadConfig(object? _ = null, [UsedImplicitly] FileSystemEventArgs? __ = null)
    {
        Config.Load(out var warnings).CopyTo(ref _config);

        foreach (var warning in warnings)
        {
            Console.Error.WriteLine(warning.Message);
            Debug.WriteLine(warning);
        }

        if (warnings.Aggregate() is { } aggregate)
            "Failed to load config!".ShowError(aggregate.Messages(), Window.Handle);

        if (IsWindows() || IsMacOS() || IsLinux() || IsFreeBSD()) // `IsBorderless` is only supported on DesktopGL.
            Window.IsBorderless = _config.Borderless;

        _animations
           .Change((Sprite.Keys.First)_config.Inverted.ToByte())
           .Change((Sprite.Keys.Second)_config.Inverted.ToByte())
           .Change((Sprite.Keys.Third)_config.Inverted.ToByte())
           .Change((Sprite.Keys.Fourth)_config.Inverted.ToByte());
    }
}
