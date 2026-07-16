// SPDX-License-Identifier: LicenseRef-PolyForm-Strict
namespace Escapey.Domains;

using static Sprite.Mouth;

// ReSharper disable InconsistentNaming
sealed partial class Config : IDisposable
{
    /// <summary>The number of columns.</summary>
    public const int ColumnCount = 4;

    /// <summary>Contains the contents for the last time the config file was read.</summary>
    static string? s_lastContents;

    /// <summary>Gets the folder where the config is stored.</summary>
    public static string Folder { get; } = Environment.GetEnvironmentVariable("ESCAPEY_CONFIG") ??
        Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(Escapey));

    /// <summary>Gets the path to the config file.</summary>
    public static string TextFile { get; } = Path.Join(Folder, "Escapey.cfg");

    /// <summary>Gets the phonemes to train and predict.</summary>
    public static ImmutableArray<Sprite.Mouth> Mouths { get; } = [Sprite.Mouth.Upset, Ah, Dz, E, F, M, Nsl, O];

    [Description(
         """
         Hi, welcome to the configuration file!
         Here you will see a list of key-value pairs to configure the application to your liking.
         All keys and values are case-insensitive and surrounding whitespace is disregarded.
         If a key is left unspecified or is set to no value, the default value is used.
         The default value will be denoted in square brackets within these comments.
         If an option is confusing, leave it as the default value.
         If this file is saved, or you drop a file into the application, it will perform a hot-reload.

         ========================
             PRIMARY CONTROLS
         ========================

         Possible values: [smart], sdl, evdev, user32.
         The method in which the application captures input.
         Smart uses user32 when running on Windows, evdev when running on Linux/BSD, otherwise SDL.
         For both primary and secondary controls, the possible values depend on this value.
         Refer to the bottom portion of this document for the list of valid values from both.
         Additionally, you can denote multiple by separating multiple values with commas.
         """
     ), JsonPropertyOrder(0)]
    public InputProvider Input { get; [UsedImplicitly] private set; } = InputProvider.Default();

    [Description(
         """

         When any of these buttons are pressed, the respective column is activated.
         """
     ), JsonPropertyOrder(1)]
    public List<string> First { get; private set; } = ["A"];

    [JsonPropertyOrder(2)]
    public List<string> Second { get; private set; } = ["S"];

    [JsonPropertyOrder(3)]
    public List<string> Third { get; private set; } = ["K"];

    [JsonPropertyOrder(4)]
    public List<string> Fourth { get; private set; } = ["L"];

    [Description(
         """

         When any of these buttons are pressed, the non-talkative expression changes to the respective emotion.
         """
     ), JsonPropertyOrder(5)]
    public List<string> Angry { get; private set; } = ["F13"];

    [JsonPropertyOrder(6)]
    public List<string> Bored { get; private set; } = ["F14"];

    [JsonPropertyOrder(7)]
    public List<string> Concentrated { get; private set; } = ["F15"];

    [JsonPropertyOrder(8)]
    public List<string> Confused { get; private set; } = ["F16"];

    [JsonPropertyOrder(9)]
    public List<string> Frown { get; private set; } = ["F17"];

    [JsonPropertyOrder(10)]
    public List<string> Happy { get; private set; } = ["F18"];

    [JsonPropertyOrder(11)]
    public List<string> HappyEyebrow { get; private set; } = ["F19"];

    [JsonPropertyOrder(12)]
    public List<string> Hide { get; private set; } = ["F20 ; This toggles their keyboard."];

    [JsonPropertyOrder(13)]
    public List<string> Laughter { get; private set; } = ["F21"];

    [JsonPropertyOrder(14)]
    public List<string> Rainbow { get; private set; } = ["F22 ; This toggles RGB eyes and mouth."];

    [JsonPropertyOrder(15)]
    public List<string> Scared { get; private set; } = ["F23"];

    [JsonPropertyOrder(16)]
    public List<string> Upset { get; private set; } = ["F24"];

    [Description(
         """

         =================
             LIP SYNC
         =================

         Possible values: [null], alc, pipewire.
         The method in which the application captures your microphone.
         Null disables audio capturing, and PipeWire is exclusive to Linux/BSD.
         Audio capture should almost always be ALC, including on Linux.
         However older systems may fail to initialize ALC when the application is run with root privileges. 
         """
     ), JsonPropertyOrder(17)]
    public AudioProvider Audio { get; [UsedImplicitly] private set; } = AudioProvider.Default();

    [Description(
         """

         If "Audio" is set to null, skip this option.
         The name of the model, referring to which file to read. This allows you to store multiple models.
         This option cannot be hot-reloaded, if you change this value you must relaunch the application.
         Any valid file name is accepted, defaulting to [main].
         """
     ), JsonPropertyOrder(18)]
    public string Profile { get; private set; } = "Main";

    [Description(
         """

         If "Audio" is set to null, skip this option.
         Possible values: Any non-zero positive number, defaulting to [1].
         The step size between slices windows of training data.
         The higher the number, the more variance the training dataset provides,
         but the less training data it is able to feed into the model.
         """
     ), JsonPropertyOrder(19)]
    public ushort TrainingSkip { get; [UsedImplicitly] private set; } = 1;

    [Description(
         """

         If "Audio" is set to null, skip this option.
         Possible values: Any non-zero positive number, defaulting to [200].
         The amount of samples the model is given at a time.
         This also dictates the frequency in which the model gets polled.
         The higher the number, the more detail the model can work with,
         but the less training data will be produced in the same amount of time during setup.
         """
     ), JsonPropertyOrder(20)]
    public ushort TrainingLength { get; [UsedImplicitly] private set; } = 200;

    [Description(
         """

         If "Audio" is set to null, skip this option.
         Possible values: Any non-zero positive number, defaulting to [5].
         The amount of previous guesses to take the average of which mouth shape should be used.
         If this value is set too high, animations may appear delayed or unresponsive.
         If too low, animations may be overly responsive and jittery.
         For a more intuitive understanding of how this works, enable the visualization.
         """
     ), JsonPropertyOrder(21)]
    public ushort Stabilize { get; private set; } = 5;

    [Description(
         """

         ==================
             APPEARANCE
         ==================

         The background color, which can either be the name of a CSS Named Color,
         or a 3/6 digit hex color. For the list of named colors, see here:
         https://developer.mozilla.org/en-US/docs/Web/CSS/named-color
         Defaults to [44475a].
         """
     ), JsonPropertyOrder(22)]
    public ParsableColor Background { get; private set; } = ParsableColor.Parse("44475a", CultureInfo.InvariantCulture);

    [Description(
         """

         The directory containing the "Fonts" and "Sprites" subdirectory to load graphics from.
         For images, supported formats include: bmp, dds, gif, jpeg, jpg, png, tif, and xnb.
         This must be a relative path. Defaults to [].
         """
     ), JsonPropertyOrder(23)]
    public string Skin { get; private set; } = "";

    [Description(
         """

         Possible values: [true], false.
         Should the leftmost button be considered:
         - ...from our point of view? (true)
         - ...from their point of view? (false)
         """
     ), JsonPropertyOrder(24)]
    public bool Inverted { get; private set; } = true;

    [Description(
         """

         Possible values: Any number between 0 and [1].
         Determines the brightness of the eyes and face when rainbow mode is on.
         """
     ), JsonPropertyOrder(25)]
    public float RainbowBrightness
    {
        get;
        [UsedImplicitly] private set => field = float.Clamp(value, 0, 1);
    } = 1;

    [Description(
         """

         Possible values: Any number between 0 and [1].
         Determines the saturation of the eyes and face when rainbow mode is on.
         """
     ), JsonPropertyOrder(26)]
    public float RainbowSaturation
    {
        get;
        [UsedImplicitly] private set => field = float.Clamp(value, 0, 1);
    } = 1;

    [Description(
         """

         Possible values: Any number between 0 and [1].
         Determines the speed in which the rainbow colors cycle through.
         """
     ), JsonPropertyOrder(27)]
    public float RainbowSpeed
    {
        get;
        [UsedImplicitly] private set => field = float.Clamp(value, 0, 1);
    } = 1;

    [Description(
         """

         ======================
             VISUALIZATIONS
         ======================

         If "Audio" is set to null, skip this option.
         The color of the frequency spectrum, which can either be the name of a CSS Named Color,
         or a 3/6 digit hex color. For the list of named colors, see here:
         https://developer.mozilla.org/en-US/docs/Web/CSS/named-color
         Defaults to [transparent], effectively disabling this feature.
         """
     ), JsonPropertyOrder(28)]
    public ParsableColor FrequencyGraph { get; private set; } = Color.Transparent;

    [Description(
         """

         Possible values: Any number between 0 and 1.
         By default, the value is [0.2].
         The width of the last guesses visualization relative to the entire screen.
         """
     ), JsonPropertyOrder(29)]
    public float OrderWidth
    {
        get;
        [UsedImplicitly] private set => field = float.Clamp(value, 0, 1);
    } = 0.2f;

    [Description(
         """

         Possible values: Any number between 0 and 1.
         By default, the value is [0.8].
         The width of the frequency spectrum relative to the entire screen.
         """
     ), JsonPropertyOrder(30)]
    public float FrequencyWidth
    {
        get;
        [UsedImplicitly] private set => field = float.Clamp(value, 0, 1);
    } = 0.8f;

    [Description(
         """

         Any non-zero positive number is accepted, defaulting to [1].
         The zoom factor for the frequency spectrum.
         1 shows what the model receives as input, but makes the lower frequencies harder to see.
         """
     ), JsonPropertyOrder(31)]
    public byte FrequencyScale { get; private set; } = 1;

    /// <inheritdoc />
    public void Dispose()
    {
        Audio.Dispose();
        Input.Dispose();

        foreach (var properties in typeof(Config).GetProperties().AsSpan())
            if (properties.GetValue(this) is List<string> list)
                list.Clear();
    }

    public void Read(string? path, out ImmutableArray<Exception> warnings)
    {
        path ??= TextFile;
        var isNewFile = !File.Exists(path);

        if (Go(() => _ = Path.GetDirectoryName(path) is { } n ? Directory.CreateDirectory(n) : null, out var dir))
        {
            warnings = [dir];
            BindInputs();
            return;
        }

        if (Go(() => File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite), out var file, out var ok))
        {
            warnings = [file];
            BindInputs();
            return;
        }

        if (isNewFile)
        {
            BindInputs();
            using StreamWriter writer = new(ok);
            warnings = Go(writer.Write, ToString(), out var write) ? [write] : [];
            return;
        }

        using StreamReader reader = new(ok);

        if (Go(reader.ReadToEnd, out var read, out var contents))
        {
            warnings = [read];
            BindInputs();
            return;
        }

        if (contents == s_lastContents)
        {
            warnings = [];
            return;
        }

        Dispose();
        Kvp.Deserialize(s_lastContents = contents, this);
        warnings = [];
        BindInputs();
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"""
         {Kvp.Serialize(this)}
         # ==========================
         #     VALID INPUT VALUES
         # ==========================

         # Below are all valid values for inputs, based on the default input handler.
         # Assigning to underscore does nothing, it is done here to enable auto-complete in text editors.
         # You can choose to omit the prefix "Key." for SDL or "KEY_" for evdev.
         _ = {Input.GetValidValues()}

         """;

    /// <summary>Captures audio and creates training data from it.</summary>
    /// <returns>The training data.</returns>
    [UnsupportedOSPlatform("android")]
    public ConcurrentBag<AudioSegment> Capture()
    {
        ConcurrentBag<AudioSegment> bag = [];
        List<Task> tasks = [with(TrainingLength)];
        var previous = new float[AudioProvider.Length];
        var ipa = Environment.GetEnvironmentVariable("ESCAPEY_IPA").OrEmpty();

        IList<(Sprite.Mouth Mouth, string Phoneme)> phonemes =
            [..Mouths.SelectMany(x => x.ToIPAs().Select(y => (x, y)))];

        if (ipa.Contains(','))
            phonemes.Retain(x => ipa.SplitOn(',').Any(y => y.Span.Trim().Trim('"').SequenceEqual(x.Phoneme)));

        foreach (var (mouth, phoneme) in phonemes)
        {
            Console.Write($"Press any button and make \"{phoneme}\" until the next prompt.");
            CaptureTrainingData(bag, tasks, previous, mouth);
            Console.WriteLine();
        }

        var cursor = Console.CursorLeft;
        var upper = AudioProvider.Length / TrainingSkip;
        var capacity = phonemes.Count * TrainingLength * upper;

        while (bag.Count < capacity)
        {
            Console.CursorLeft = cursor;
            Console.Write($"Processing {bag.Count} / {capacity}.");
            Thread.Sleep(10);
        }

        Console.CursorLeft = cursor;
        Console.WriteLine($"Done processing {bag.Count} / {capacity}.");
        return bag;
    }

    /// <summary>Adds every view between the two audio buffers captured to the bag.</summary>
    /// <param name="bag">The bag to add the views to.</param>
    /// <param name="tasks">The list to add the immediately-running tasks that execute the FFT transforms.</param>
    /// <param name="prev">The previous audio data.</param>
    /// <param name="mouth">The current mouth.</param>
    void AddWindows(ConcurrentBag<AudioSegment> bag, ICollection<Task> tasks, float[] prev, Sprite.Mouth mouth)
    {
        var raw = Audio.WaitForRaw().ToImmutableArray();
        var upper = AudioProvider.Length / TrainingSkip;

        for (var i = 0; i < upper; i++)
        {
            var temp = AudioProvider.Temporary();
            var tempRaw = temp.PollRaw();
            prev.AsSpan(..^i).CopyTo(tempRaw);
            raw.AsSpan().UnsafelyTake(i).CopyTo(tempRaw[^i..]);

            void Add()
            {
                bag.Add((temp.Poll() ?? temp.Segment).With(mouth));
                temp.Dispose();
            }

            async Task? AddAsync()
            {
                await Task.Yield();
#if DEBUG
                if (Go(Add, out var e))
                    EscapeyGame.Log(e);
#else
                Add();
#endif
            }

            tasks.Add(Task.Run(AddAsync));
        }

        raw.CopyTo(prev);
    }

    /// <summary>Binds properties representing <see cref="Columns"/> onto the <see cref="InputProvider"/>.</summary>
    void BindInputs()
    {
        Input.Clear();

        foreach (var column in Enum.GetValues<Columns>())
            if (typeof(Config).GetProperty(column.ToString())?.GetValue(this) is List<string> list)
                foreach (var next in list.AsSpan())
                    if (!Input.Add(column, next))
                        EscapeyGame.Log(new FormatException($"Not a valid {column} input value: {next}"));
    }

    /// <summary>Captures one set of training data.</summary>
    /// <param name="bag">The bag to add the views to.</param>
    /// <param name="tasks">The list to add the immediately-running tasks that execute the FFT transforms.</param>
    /// <param name="prev">The previous audio data.</param>
    /// <param name="mouth">The current mouth.</param>
    [UnsupportedOSPlatform("android")]
    void CaptureTrainingData(ConcurrentBag<AudioSegment> bag, ICollection<Task> tasks, float[] prev, Sprite.Mouth mouth)
    {
        var cursor = Console.CursorLeft;
        Console.Write($" 0 / {TrainingLength}");
        Console.ReadKey();
        Audio.WaitForRaw().CopyTo(prev);

        for (var i = 0; i < TrainingLength; i++)
        {
            Console.CursorLeft = cursor;
            Console.Write($" {i + 1} / {TrainingLength}");
            AddWindows(bag, tasks, prev, mouth);
        }
    }
}
