// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Domains;

/// <summary>Represents the configuration of the application.</summary>
/// <param name="borderless">Whether to use a borderless window.</param>
/// <param name="inverted">Whether to invert the columns.</param>
/// <param name="speed">The speed of the rainbow.</param>
/// <param name="stabilize">The number of frames to display before allowing animations to change.</param>
/// <param name="training">The amount of training data per phoneme.</param>
/// <param name="profile">The name of the model.</param>
/// <param name="background">The background color.</param>
/// <param name="audio">The audio provider.</param>
/// <param name="input">The input provider.</param>
sealed partial class Config(
    bool borderless,
    bool inverted,
    int speed,
    int stabilize,
    int training,
    string profile,
    Color background,
    IAudioProvider audio,
    IInputProvider input
) : IDisposable
{
    /// <summary>The number of columns.</summary>
    public const int ColumnCount = 4;

    /// <summary>The default value for <see cref="Speed"/>.</summary>
    const int DefaultSpeed = 4;

    /// <summary>The default value for <see cref="Stabilize"/>.</summary>
    const int DefaultStabilize = 3;

    /// <summary>The default value for <see cref="Training"/>.</summary>
    const int DefaultTraining = 20;

    /// <summary>The default value for <see cref="Profile"/>.</summary>
    const string DefaultProfile = "main.mlnet";

    /// <summary>The aliases for <see cref="Color"/> instances.</summary>
    static readonly FrozenDictionary<string, Color>.AlternateLookup<ReadOnlySpan<char>> s_knownColors = typeof(Color)
       .GetProperties(BindingFlags.Public | BindingFlags.Static)
       .Where(x => x.CanRead && x.PropertyType == typeof(Color) && x.GetIndexParameters() is [])
       .SelectMany(IncludeAlternateSpellings)
       .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase)
       .GetAlternateLookup<ReadOnlySpan<char>>();

    /// <summary>Parses the syntax.</summary>
    static readonly SearchValues<char>
        s_assignment = SearchValues.Create("="),
        s_comment = SearchValues.Create("#"),
        s_invalidFileName = SearchValues.Create(Path.GetInvalidFileNameChars()),
        s_separator = SearchValues.Create(",");

    /// <summary>Contains the contents for the last time the config file was read.</summary>
    static string? s_lastContents;

    /// <summary>Sets appropriate environment variables to ensure providers will always work.</summary>
    static Config()
    {
        Environment.SetEnvironmentVariable("LC_ALL", "en.US.UTF-8");

        if ((OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD()) &&
            (Environment.GetEnvironmentVariable("SUDO_UID") ?? $"{Euid()}") is var uid)
            Environment.SetEnvironmentVariable("XDG_RUNTIME_DIR", $"/run/user/{uid}");
    }

    /// <summary>Gets the folder where the config is stored.</summary>
    public static string Folder { get; } =
        Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(Escapey));

    /// <summary>Gets the path to the config file.</summary>
    public static string TextFile { get; } = Path.Join(Folder, "config.ini");

    /// <summary>Gets value determining whether to use borderless mode.</summary>
    public bool Borderless { get; private set; } = borderless;

    /// <summary>Gets a value determining whether to invert the columns.</summary>
    public bool Inverted { get; private set; } = inverted;

    /// <summary>Gets the speed of the rainbow.</summary>
    public int Speed { get; private set; } = speed;

    /// <summary>Gets the number of frames to display before allowing animations to change.</summary>
    public int Stabilize { get; private set; } = stabilize;

    /// <summary>Gets the amount of training data per phoneme.</summary>
    public int Training { get; private set; } = training;

    /// <summary>Gets the name of the model.</summary>
    public string Profile { get; private set; } = profile;

    /// <summary>Gets the background color.</summary>
    public Color Background { get; private set; } = background;

    /// <summary>Gets the audio provider.</summary>
    public IAudioProvider Audio { get; private set; } = audio;

    /// <summary>Gets the input provider.</summary>
    public IInputProvider Input { get; private set; } = input;

    /// <summary>Loads the config.</summary>
    /// <param name="warnings">The warnings.</param>
    /// <returns>The config.</returns>
    public static Config Load(out ImmutableArray<Exception> warnings)
    {
        static Config Default(Exception initial, out ImmutableArray<Exception> warnings)
        {
#pragma warning disable IDISP001
            var a = IAudioProvider.Default();
            var i = IInputProvider.Default(out var inputWarnings);
#pragma warning restore IDISP001
            var builder = ImmutableArray.CreateBuilder<Exception>();
            builder.Add(initial);
            builder.AddRange(inputWarnings);
            warnings = builder.DrainToImmutable();
            return new(false, false, DefaultSpeed, DefaultStabilize, DefaultTraining, DefaultProfile, default, a, i);
        }

        // ReSharper disable once NullableWarningSuppressionIsUsed
        if (Go(() => _ = Path.GetDirectoryName(TextFile) is { } n ? Directory.CreateDirectory(n) : null, out var dir))
            return Default(dir, out warnings);

        if (Go(() => File.Open(TextFile, FileMode.OpenOrCreate), out var file, out var ok))
            return Default(file, out warnings);

        using StreamReader stream = new(ok);

        if (stream.ReadToEnd() is var str && Parse(str, out warnings) is var ret && str.Equals(s_lastContents))
            warnings = []; // We suppress the warnings if the contents didn't change.

        s_lastContents = str;
        return ret;
    }

    /// <summary>Parses the config string.</summary>
    /// <param name="str">The config string.</param>
    /// <param name="warnings">The warnings.</param>
    /// <returns>The config.</returns>
    // ReSharper disable once CognitiveComplexity
    public static Config Parse(scoped ReadOnlySpan<char> str, out ImmutableArray<Exception> warnings)
    {
        Color color = default;
        IAudioProvider? audio = null;
        IInputProvider? input = null;
        var profile = DefaultProfile;
        ImmutableArray<Exception> defaultWarnings = [];
        var accumulator = ImmutableArray.CreateBuilder<Exception>();
        int speed = DefaultSpeed, stabilize = DefaultStabilize, training = DefaultTraining;
        bool borderless = false, implicitInput = false, inverted = false;
#pragma warning disable IDISP003
        foreach (var line in str.SplitLines())
            _ = SplitKeyValuePair(line, out var value) switch
            {
                "" => default,
                var x when x.EqualsIgnoreCase(nameof(Audio)) => ChangeAudio(value, accumulator, ref audio),
                var x when x.EqualsIgnoreCase(nameof(Borderless)) => ChangeBoolean(value, accumulator, ref borderless),
                var x when x.EqualsIgnoreCase(nameof(Color)) || x.EqualsIgnoreCase("colour") =>
                    ChangeColor(value, accumulator, ref color),
                var x when x.EqualsIgnoreCase(nameof(Input)) => ChangeInput(value, implicitInput, accumulator, ref input),
                var x when x.EqualsIgnoreCase(nameof(Inverted)) => ChangeBoolean(value, accumulator, ref inverted),
                var x when x.EqualsIgnoreCase(nameof(Profile)) => ChangeProfile(value, accumulator, ref profile),
                var x when x.EqualsIgnoreCase(nameof(Speed)) => ChangeNumber(value, accumulator, ref speed),
                var x when x.EqualsIgnoreCase(nameof(Stabilize)) || x.EqualsIgnoreCase("stabilise") =>
                    ChangeNumber(value, accumulator, ref stabilize),
                var x when x.EqualsIgnoreCase(nameof(Training)) => ChangeNumber(value, accumulator, ref training),
                var x when x.TryIntoEnum<Columns>() is { } column =>
                    AddColumn(column, value, accumulator, ref implicitInput, ref input),
                var x => UnrecognizedKey(x, accumulator),
            };
#pragma warning restore IDISP003
        audio ??= IAudioProvider.Default();
        input ??= IInputProvider.Default(out defaultWarnings);
        accumulator.AddRange(defaultWarnings);
        warnings = accumulator.DrainToImmutable();
        return new(borderless, inverted, speed, stabilize, training, profile, color, audio, input);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Audio.Dispose();
        Input.Dispose();
    }

    /// <summary>Copies the config over to <paramref name="config"/>.</summary>
    /// <param name="config"></param>
    public void CopyTo([MustDisposeResource, NotNull] ref Config? config)
    {
        if (config is null)
        {
            config = this;
            return;
        }

        config.Dispose();
        config.Audio = Audio;
        config.Background = Background;
        config.Borderless = Borderless;
        config.Input = Input;
        config.Inverted = Inverted;
        config.Profile = Profile;
        config.Speed = Speed;
        config.Stabilize = Stabilize;
        config.Training = Training;
    }

    /// <summary>Attempts to parse a hex number from the provided inputs.</summary>
    /// <param name="fst">The first character.</param>
    /// <param name="snd">The second character.</param>
    /// <returns>The parsed number if successful; otherwise, <see langword="null"/>.</returns>
    static byte? P(char fst, char snd = '\0') =>
        byte.TryParse([fst, snd is '\0' ? fst : snd], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var ret)
            ? ret
            : null;

    /// <summary>Attempts to parse a color from the provided inputs.</summary>
    /// <param name="chars">The characters to parse.</param>
    /// <returns>The parsed color if successful; otherwise, <see langword="null"/>.</returns>
    static Color? Parse(ReadOnlySpan<char> chars) =>
        chars switch
        {
            _ when s_knownColors.TryGetValue(chars, out var knownColor) => knownColor,
            [var r, var g, var b] when (P(r), P(b), P(g)) is ({ } pr, { } pb, { } pg) => new(pr, pg, pb),
            [var r, var g, var b, var a] when (P(r), P(b), P(g), P(a)) is ({ } pr, { } pb, { } pg, { } pa)
                => new(pr, pb, pg, pa),
            [var r, var rp, var g, var gp, var b, var bp]
                when (P(r, rp), P(g, gp), P(b, bp)) is ({ } pr, { } pb, { } pg) => new(pr, pg, pb),
            [var r, var rp, var g, var gp, var b, var bp, var a, var ap]
                when (P(r, rp), P(g, gp), P(b, bp), P(a, ap)) is ({ } pr, { } pb, { } pg, { } pa)
                => new(pr, pg, pb, pa),
            _ => null,
        };

    /// <summary>Includes alternate spellings for <see cref="Color.Gray"/> and similar.</summary>
    /// <param name="x">The property to include.</param>
    /// <returns>The included properties.</returns>
    static IEnumerable<KeyValuePair<string, Color>> IncludeAlternateSpellings(PropertyInfo x) =>
        (x.GetValue(null) as Color?).GetValueOrDefault() is var color &&
        x.Name.Contains(nameof(Color.Gray), StringComparison.InvariantCultureIgnoreCase)
            ? [new(x.Name, color), new(x.Name.Replace(nameof(Color.Gray), "Grey"), color)]
            : [new(x.Name, color)];

    /// <summary>Splits a line into a key and value.</summary>
    /// <param name="line">The line to split.</param>
    /// <param name="values">The value section.</param>
    /// <returns>The key section.</returns>
    static ReadOnlySpan<char> SplitKeyValuePair(ReadOnlySpan<char> line, out ReadOnlySpan<char> values)
    {
        if (line is [var first, ..] && s_comment.Contains(first))
            return values = default;

        var (key, value) = line.SplitOn(s_comment).First.SplitOn(s_assignment);
        values = value.Body.Trim();
        return key.Trim();
    }

    /// <summary>Associates an input with the specified <see cref="Columns"/>.</summary>
    /// <param name="column">The <see cref="Columns"/> to associate.</param>
    /// <param name="value">The value that represents the input.</param>
    /// <param name="accumulator">The accumulator of warnings.</param>
    /// <param name="implicitInput">The implicit input flag to potentially coerce.</param>
    /// <param name="input">The input provider to potentially coerce.</param>
    /// <returns><c>()</c></returns>
    static ValueTuple AddColumn(
        Columns column,
        scoped ReadOnlySpan<char> value,
        ImmutableArray<Exception>.Builder accumulator,
        ref bool implicitInput,
        ref IInputProvider? input
    )
    {
        if (input is null)
        {
            input = IInputProvider.Default(out var defaultWarnings);
            accumulator.AddRange(defaultWarnings);
            implicitInput = true;
        }

        if (!input.Add(column, value.SplitOn(s_separator)))
            accumulator.Add(new FormatException($"Unrecognized inputs, ignoring invalid values: {value}"));

        return default;
    }

    /// <summary>Changes the audio provider.</summary>
    /// <param name="value">The alias to use.</param>
    /// <param name="accumulator">The accumulator of warnings.</param>
    /// <param name="audio">The audio provider to change.</param>
    /// <returns><c>()</c></returns>
    static ValueTuple ChangeAudio(
        ReadOnlySpan<char> value,
        ImmutableArray<Exception>.Builder accumulator,
        [MustDisposeResource, NotNull] ref IAudioProvider? audio
    )
    {
        audio?.Dispose();
        audio = IAudioProvider.FromAlias(value, out var audioWarnings);
        accumulator.AddRange(audioWarnings);
        return default;
    }

    /// <summary>Changes the boolean flag.</summary>
    /// <param name="value">The new value.</param>
    /// <param name="accumulator">The accumulator of warnings.</param>
    /// <param name="flag">The flag to change.</param>
    /// <returns><c>()</c></returns>
    static ValueTuple ChangeBoolean(
        scoped ReadOnlySpan<char> value,
        ImmutableArray<Exception>.Builder accumulator,
        ref bool flag
    )
    {
        if (bool.TryParse(value, out var b))
            flag = b;
        else
            accumulator.Add(new FormatException($"Must be true or false, ignoring: {value}"));

        return default;
    }

    /// <summary>Changes the background color.</summary>
    /// <param name="value">The new value.</param>
    /// <param name="accumulator">The accumulator of warnings.</param>
    /// <param name="color">The background color to change.</param>
    /// <returns><c>()</c></returns>
    static ValueTuple ChangeColor(
        scoped ReadOnlySpan<char> value,
        ImmutableArray<Exception>.Builder accumulator,
        ref Color color
    )
    {
        if (Parse(value) is { } c)
            color = c;
        else
            accumulator.Add(new FormatException($"Unrecognized color, ignoring: {value}"));

        return default;
    }

    /// <summary>Changes the input provider.</summary>
    /// <param name="value">The alias to use.</param>
    /// <param name="implicitInput">Whether to warn if the input provider is not defined.</param>
    /// <param name="accumulator">The accumulator of warnings.</param>
    /// <param name="input">The input provider to change.</param>
    /// <returns><c>()</c></returns>
    static ValueTuple ChangeInput(
        scoped ReadOnlySpan<char> value,
        bool implicitInput,
        ImmutableArray<Exception>.Builder accumulator,
        ref IInputProvider? input
    )
    {
        if (implicitInput)
        {
            accumulator.Add(new FormatException($"Define the input provider before keybindings, ignoring: {value}."));
            return default;
        }

        input?.Dispose();
        input = IInputProvider.FromAlias(value, out var inputWarnings);
        accumulator.AddRange(inputWarnings);
        return default;
    }

    /// <summary>Changes the number.</summary>
    /// <param name="value">The new value.</param>
    /// <param name="accumulator">The accumulator of warnings.</param>
    /// <param name="number">The number to change.</param>
    /// <returns><c>()</c></returns>
    static ValueTuple ChangeNumber(
        ReadOnlySpan<char> value,
        ImmutableArray<Exception>.Builder accumulator,
        ref int number
    )
    {
        if (value.TryInto<int>() is not { } t)
            accumulator.Add(new FormatException($"Invalid number, ignoring invalid value: {value}"));
        else if (t <= 0)
            accumulator.Add(new FormatException($"Number must be positive, ignoring invalid value: {value}"));
        else
            number = t;

        return default;
    }

    /// <summary>Changes the profile.</summary>
    /// <param name="value">The new value.</param>
    /// <param name="accumulator">The accumulator of warnings.</param>
    /// <param name="profile">The profile to change.</param>
    /// <returns><c>()</c></returns>
    static ValueTuple ChangeProfile(
        ReadOnlySpan<char> value,
        ImmutableArray<Exception>.Builder accumulator,
        ref string profile
    )
    {
        if (value.ContainsAny(s_invalidFileName))
            accumulator.Add(new FormatException($"Invalid file name, ignoring invalid value: {value}"));
        else
            profile = $"{value}.mlnet";

        return default;
    }

    /// <summary>Adds an exception to the accumulator.</summary>
    /// <param name="key">The key that was unrecognized.</param>
    /// <param name="accumulator">The accumulator of warnings.</param>
    /// <returns><c>()</c></returns>
    static ValueTuple UnrecognizedKey(scoped ReadOnlySpan<char> key, ImmutableArray<Exception>.Builder accumulator)
    {
        accumulator.Add(new FormatException($"Unrecognized key, ignoring: {key}"));
        return default;
    }

    [LibraryImport("c", EntryPoint = "geteuid"),
     SupportedOSPlatform("macos"),
     SupportedOSPlatform("linux"),
     SupportedOSPlatform("freebsd")]
    private static partial uint Euid();
}
