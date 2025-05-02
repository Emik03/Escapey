// SPDX-License-Identifier: MPL-2.0
namespace Escapey;

/// <summary>The application for drawing the adorable character Escapey.</summary>
[CLSCompliant(false)]
public sealed partial class EscapeyGame() : Letterboxed2DGame(930, 779, 0.5f)
{
    static readonly bool s_silent = bool.TryParse(Environment.GetEnvironmentVariable("ESCAPEY_SILENT"), out var b) && b;

    /// <summary>The config.</summary>
    readonly Config _config = new();

    /// <summary>The set of animations.</summary>
    // ReSharper disable NullableWarningSuppressionIsUsed
    Animations _animations = null!;

    /// <summary>The file system watcher for the config.</summary>
    FileSystemWatcher _watcher = null!;

    /// <summary>The model for speech recognition.</summary>
    HearMonitor _hearMonitor = null!;

    /// <summary>The mouth to use when the user is not speaking.</summary>
    Sprite.Mouth _neutral = Sprite.Mouth.Happy;

    /// <summary>Manages toggle states.</summary>
    Toggle _rainbow, _visible;

    /// <inheritdoc />
    public override BlendState BatchBlendState =>
        _config.FrequencyGraph.Result.A is 0 ? BlendState.AlphaBlend : BlendState.NonPremultiplied;

    /// <summary>Runs the game.</summary>
    public static void Go()
    {
        using EscapeyGame game = new();
        game.Run();
    }

    /// <summary>Prints each exception to the console.</summary>
    /// <param name="exceptions">The exceptions to log.</param>
    public static void Log(ReadOnlySpan<Exception> exceptions)
    {
        if (s_silent)
            return;

        foreach (var ex in exceptions)
        {
            Console.Error.WriteLine($"{ex.Message}\n{ex.StackTrace.SplitLines()[..3]}");
            Debug.WriteLine(ex);
        }
    }

    /// <inheritdoc />
    protected override void Initialize()
    {
        base.Initialize();

        if (IsDesktop)
            Window.IsBorderless = true;

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
           .Add<Sprite.Arm.Left>()
           .Add<Sprite.Arm.Right>()
           .Add<Sprite.LaughterMarks>()
           .Sync<Sprite.Legs, Sprite.Arm.Left>();

        _watcher = new(Path.GetDirectoryName(Config.TextFile).OrEmpty(), Path.GetFileName(Config.TextFile))
        {
            EnableRaisingEvents = true,
        };

        LoadConfig();
        _hearMonitor = HearMonitor.From(this, _config);
        Window.FileDrop += LoadConfig;
        _watcher.Changed += LoadConfig;
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
        var sound = _hearMonitor.Poll();
        var columns = _config.Input.Poll().InvertIf(_config.Inverted);
        var count = columns.ButtonCount();
        var pushed = columns.Has(Columns.Hide);
        var toggled = _visible.Accept(!pushed);
        var brightness = (byte)(_config.RainbowBrightness * byte.MaxValue);
        var saturation = (byte)(_config.RainbowSaturation * byte.MaxValue);
        var time = (int)(gameTime.TotalGameTime.Ticks * _config.RainbowSpeed / TimeSpan.TicksPerMillisecond);
        var color = _rainbow.Accept(columns.Has(Columns.Rainbow)) ? time.ToColor(saturation, brightness) : Color.White;
        var neutral = columns.ToMouth(ref _neutral);

        _animations
           .Background(_config.Background)
           .Change(columns.ToEyes())
           .Change(toggled ? columns.ToLeftArm() : Sprite.Arm.Left.Idle)
           .Change(toggled ? columns.ToRightArm() : Sprite.Arm.Right.Idle)
           .Change(sound.IsSpeaking() ? sound : neutral)
           .Colored<Sprite.Eyes>(color)
           .Colored<Sprite.Mouth>(color)
           .SetDrawOrder<Sprite.Arm.Left>(count)
           .SetDrawOrder<Sprite.Arm.Right>(count)
           .SetDrawOrder<Sprite.Keys.Overlay>(count)
           .SetDrawOrder<Sprite.Keys.Background>(count)
           .SetDrawOrder<Sprite.Keys.First>(count)
           .SetDrawOrder<Sprite.Keys.Second>(count)
           .SetDrawOrder<Sprite.Keys.Third>(count)
           .SetDrawOrder<Sprite.Keys.Fourth>(count)
           .SetVisibility<Sprite.Keys.Overlay>(toggled)
           .SetVisibility<Sprite.Keys.Background>(toggled)
           .SetVisibility<Sprite.Keys.First>(toggled && columns.Has(Columns.First))
           .SetVisibility<Sprite.Keys.Second>(toggled && columns.Has(Columns.Second))
           .SetVisibility<Sprite.Keys.Third>(toggled && columns.Has(Columns.Third))
           .SetVisibility<Sprite.Keys.Fourth>(toggled && columns.Has(Columns.Fourth))
           .SetVisibility<Sprite.LaughterMarks>(_neutral is Sprite.Mouth.Laughter)
           .Draw(gameTime);

        _hearMonitor.Draw(gameTime);
    }

    /// <inheritdoc />
    protected override void Update(GameTime gameTime) { }

    /// <summary>Loads or reloads the config.</summary>
    /// <param name="path">The sender, optionally containing the file to load.</param>
    /// <param name="__">The event args, ignored.</param>
    void LoadConfig(object? path = null, [UsedImplicitly] FileSystemEventArgs? __ = null)
    {
        _config.Read(path as string, out var warnings);
        Log(warnings.AsSpan());

        _animations
           .Change((Sprite.Keys.First)_config.Inverted.ToByte())
           .Change((Sprite.Keys.Second)_config.Inverted.ToByte())
           .Change((Sprite.Keys.Third)_config.Inverted.ToByte())
           .Change((Sprite.Keys.Fourth)_config.Inverted.ToByte());
    }

    /// <summary>Loads or reloads the config.</summary>
    /// <param name="_">The sender, ignored.</param>
    /// <param name="e">The event args, used for determining which file was dropped.</param>
    void LoadConfig([UsedImplicitly] object? _, FileDropEventArgs e)
    {
        if (e.Files is [var file])
            LoadConfig(file);
    }
}
