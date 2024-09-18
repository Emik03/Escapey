// SPDX-License-Identifier: MPL-2.0
namespace Escapey;

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
    Sprite.Mouth _neutral;

    /// <summary>Whether the keys are visible.</summary>
    bool _areKeysVisible = true;

    /// <summary>The state of the key visibility.</summary>
    bool? _keyVisibleState;

    /// <summary>Initializes a new instance of the <see cref="EscapeyGame"/> class.</summary>
    public EscapeyGame()
    {
        _ = new GraphicsDeviceManager(this)
        {
            GraphicsDevice = { BlendState = BlendState.NonPremultiplied },
            PreferredBackBufferWidth = 930,
            PreferredBackBufferHeight = 779,
            SynchronizeWithVerticalRetrace = true,
        };

        _animations = new Animations(this)
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
           .Add<Sprite.Arm.Left>();

        _watcher = new(Path.GetDirectoryName(Config.TextFile).OrEmpty(), Path.GetFileName(Config.TextFile))
        {
            EnableRaisingEvents = true,
        };

        LoadConfig();
        IsMouseVisible = true;
        _hearMonitor = HearMonitor.From(_config);
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
        GraphicsDevice.Clear(_config.Background);

        var columns = _config.Input.Poll().InvertIf(_config.Inverted);
        var sound = _hearMonitor.Poll();
        var flipped = false;

        _keyVisibleState = _keyVisibleState switch
        {
            true when (flipped = true) && (_areKeysVisible = !_areKeysVisible) is var _ => null,
            null when !columns.Has(Columns.Hide) => false,
            false when columns.Has(Columns.Hide) => true,
            _ => _keyVisibleState,
        };

        _animations
           .Change(columns.ToEyes())
           .Change(sound.IsSpeaking() ? sound : columns.ToMouth(ref _neutral))
           .Change(_areKeysVisible ? columns.ToLeftArm() : Sprite.Arm.Left.Idle)
           .Change(_areKeysVisible ? columns.ToRightArm() : Sprite.Arm.Right.Idle)
           .SetVisibility<Sprite.Keys.Overlay>(_areKeysVisible)
           .SetVisibility<Sprite.Keys.Background>(_areKeysVisible)
           .SetVisibility<Sprite.Keys.First>(_areKeysVisible && columns.Has(Columns.First))
           .SetVisibility<Sprite.Keys.Second>(_areKeysVisible && columns.Has(Columns.Second))
           .SetVisibility<Sprite.Keys.Third>(_areKeysVisible && columns.Has(Columns.Third))
           .SetVisibility<Sprite.Keys.Fourth>(_areKeysVisible && columns.Has(Columns.Fourth))
           .Reset<Sprite.Legs>(flipped)
           .Draw(gameTime);
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

        if (OperatingSystem.IsWindows() ||
            OperatingSystem.IsMacOS() ||
            OperatingSystem.IsLinux() ||
            OperatingSystem.IsFreeBSD()) // `IsBorderless` is only supported on DesktopGL.
            Window.IsBorderless = _config.Borderless;

        _animations
           .Change((Sprite.Keys.First)_config.Inverted.ToByte())
           .Change((Sprite.Keys.Second)_config.Inverted.ToByte())
           .Change((Sprite.Keys.Third)_config.Inverted.ToByte())
           .Change((Sprite.Keys.Fourth)_config.Inverted.ToByte());
    }
}
