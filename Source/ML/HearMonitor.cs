// SPDX-License-Identifier: MPL-2.0
namespace Escapey.ML; // ReSharper disable PossibleLossOfFraction
using static Sprite.Mouth;

/// <summary>Encapsulates the model for predicting phonemes.</summary>
/// <param name="game">The game that this component will belong to.</param>
/// <param name="ml">The machine learning context.</param>
/// <param name="transformer">The transformer for the model.</param>
/// <param name="config">The configuration.</param>
sealed class HearMonitor(Game game, MLContext ml, [HandlesResourceDisposal] ITransformer transformer, Config config)
    : DrawableGameComponent(game), IDisposable
{
    /// <summary>The phonemes to train and predict.</summary>
    static readonly ImmutableArray<Sprite.Mouth> s_mouths = [Upset, Ah, Dz, E, F, M, Nsl, O];

    /// <summary>The prediction engine.</summary>
    readonly PredictionEngine<AudioSegment, Prediction> _engine =
        ml.Model.CreatePredictionEngine<AudioSegment, Prediction>(transformer);

    /// <summary>The number of occurrences of each mouth state.</summary>
    readonly int[] _count = new int[s_mouths.Length];

    /// <summary>The sprite batch to draw with.</summary>
    readonly SpriteBatch _batch = new(game.GraphicsDevice);

    /// <summary>The font to draw with.</summary>
    readonly SpriteFont _font = game.Content.Load<SpriteFont>("Fonts/main");

    /// <summary>The audio segment to predict.</summary>
    AudioSegment _segment = new();

    /// <summary>The previous color used to draw frequency graph.</summary>
    Color _last;

    /// <summary>The current prediction.</summary>
    Prediction _prediction = new();

    /// <summary>The previous mouth states.</summary>
    Sprite.Mouth[] _order = [];

    /// <summary>The texture to draw with.</summary>
    Texture2D? _texture;

    /// <summary>Creates the new <see cref="HearMonitor"/>.</summary>
    /// <param name="game">The game that this component will belong to.</param>
    /// <param name="config">The configuration.</param>
    /// <returns>The new <see cref="HearMonitor"/>.</returns>
    [MustDisposeResource]
    public static HearMonitor From(Game game, Config config)
    {
        var modelFile = Path.Join(Config.Folder, config.Profile);
        var dataFile = Path.ChangeExtension(modelFile, ".dat");
        MLContext ml = new();

        if (File.Exists(modelFile))
            return new(game, ml, ml.Model.Load(modelFile, out _), config);

        var trainer = ml.MulticlassClassification.Trainers.OneVersusAll(
            ml.BinaryClassification.Trainers.LbfgsLogisticRegression()
        );

        var data = LoadOrSaveData(ml, config, dataFile);
        string[] features = [..AudioSegment.Length.For(x => $"E{x}"), nameof(AudioSegment.NormalizationFactor)];
#pragma warning disable IDISP001
        var transformer = ml.Transforms
#pragma warning restore IDISP001
           .ReplaceMissingValues(features.ConvertAll(x => new InputOutputColumnPair(x)))
           .Append(ml.Transforms.Concatenate("Features", features))
           .Append(ml.Transforms.Conversion.MapValueToKey(nameof(AudioSegment.Label), maximumNumberOfKeys: O - Upset))
           .Append(trainer)
           .Append(ml.Transforms.Conversion.MapKeyToValue(nameof(Prediction.PredictedLabel)))
           .Fit(data);

        ml.Model.Save(transformer, data.Schema, modelFile);
        return new(game, ml, transformer, config);
    }

    /// <summary>Polls for the current mouth.</summary>
    /// <returns>The current mouth.</returns>
    public Sprite.Mouth Poll()
    {
        if (config.Stabilize is var stabilize && _order.Length != stabilize)
        {
            _order = new Sprite.Mouth[stabilize];
            _order.AsSpan().Fill(Upset);
            _count.AsSpan().Clear();
            _count[0] = stabilize;
        }

        ref var count = ref MemoryMarshal.GetArrayDataReference(_count);

        if (config.Audio.Poll() is { } segment)
        {
            var last = _order.Length - 1;
            _engine.Predict(_segment = segment, ref _prediction);
            ref var order = ref MemoryMarshal.GetArrayDataReference(_order);
            Unsafe.Add(ref count, order - Upset)--;
            MemoryMarshal.CreateReadOnlySpan(Unsafe.Add(ref order, 1), last).CopyTo(_order);
            Unsafe.Add(ref count, (Unsafe.Add(ref order, last) = _prediction.Mouth) - Upset)++;
        }

        ref readonly var max = ref count;
        ref var step = ref Unsafe.Add(ref count, 1);
        ref readonly var end = ref Unsafe.Add(ref count, _count.Length);

        for (; Unsafe.IsAddressLessThan(step, end); step = ref Unsafe.Add(ref step, 1))
            if (max < step)
                max = ref step;

        return (Sprite.Mouth)(Unsafe.ByteOffset(count, max) / sizeof(int) + (nint)Upset);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _engine.Dispose();
            _texture?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override void Draw(GameTime gameTime)
    {
        if (!Visible || config.FrequencyGraph.A is 0 || config.FrequencyWidth is 0)
            return;

        if (_texture is null || _last != config.FrequencyGraph)
        {
            _texture?.Dispose();
            _texture = new(GraphicsDevice, 1, 1);
            _texture.SetData([_last = config.FrequencyGraph]);
        }

        ref var start = ref _segment.Head;
        ref var end = ref Unsafe.Add(ref start, AudioSegment.Length / config.FrequencyScale);
        _batch.Begin(blendState: BlendState.NonPremultiplied);

        for (var i = 0f; Unsafe.IsAddressLessThan(ref start, ref end); start = ref Unsafe.Add(ref start, 1), i++)
            _batch.Draw(_texture, Box(i, start * _segment.NormalizationFactor.Sqrt(), config.FrequencyScale), _last);

        Vector2 position = new((GraphicsDevice.Width() - 72) / 2, GraphicsDevice.Height() - 108);
        _batch.DrawString(_font, _prediction.ToString(), position, _last);
        _batch.End();
    }

    /// <summary>Loads or saves the data.</summary>
    /// <param name="ml">The machine learning context.</param>
    /// <param name="config">The configuration.</param>
    /// <param name="file">The path to the data to either save or load.</param>
    /// <returns>The <see cref="IDataView"/> containing the training date.</returns>
    static IDataView LoadOrSaveData(MLContext ml, Config config, string file)
    {
        if (File.Exists(file))
            return ml.Data.LoadFromBinary(file);

        var data = ml.Data.LoadFromEnumerable(s_mouths.Select(Capture(config)).SelectMany(x => x).ToIList());
        using var stream = File.Open(file, FileMode.OpenOrCreate);
        ml.Data.SaveAsBinary(data, stream);
        return data;
    }

    /// <summary>Creates the function for capturing audio and creating training data from it.</summary>
    /// <param name="config">The configuration.</param>
    /// <returns>The function to create training data.</returns>
    static Func<Sprite.Mouth, ImmutableArray<AudioSegment>> Capture(Config config)
    {
        float[] current = new float[IAudioProvider.Length], previous = new float[IAudioProvider.Length];
        var builder = ImmutableArray.CreateBuilder<AudioSegment>();
        IAudioProvider audio = config.Audio, blank = IAudioProvider.CreateBlank(current);
        var training = config.Training;

        void AddWindows(Sprite.Mouth mouth)
        {
            var raw = audio.WaitForRaw();

            for (var j = 0; j < IAudioProvider.Length; j++)
            {
                previous.AsSpan(..^j).CopyTo(current);
                raw.UnsafelyTake(j).CopyTo(current.AsSpan(^j));
                builder.Add((blank.Poll() ?? blank.Segment).With(mouth));
            }

            raw.CopyTo(previous);
        }

        void ProcessPhoneme(Sprite.Mouth mouth)
        {
            var cursor = Console.CursorLeft;
            Console.Write($" 0 / {training}");
            Console.ReadKey();
            audio.WaitForRaw().CopyTo(previous);

            for (var i = 0; i < training; i++)
            {
                Console.CursorLeft = cursor;
                Console.Write($" {i + 1} / {training}");
                AddWindows(mouth);
            }

            previous.AsSpan().CopyTo(current);
            builder.Add((blank.Poll() ?? blank.Segment).With(mouth));
        }

        ImmutableArray<AudioSegment> Setup(Sprite.Mouth mouth)
        {
            using var _ = blank;
            var phonemes = mouth.ToIPAs();
            builder.Capacity = phonemes.Length * (training * IAudioProvider.Length + 1);

            foreach (var phoneme in phonemes)
            {
                Console.Write($"Press any button and make \"{phoneme}\" until the next prompt.");
                ProcessPhoneme(mouth);
                Console.WriteLine();
            }

            return builder.MoveToImmutable();
        }

        return Setup;
    }

    /// <summary>Creates the box for the frequency graph.</summary>
    /// <param name="i">The index of the box.</param>
    /// <param name="amount">The length of the box.</param>
    /// <param name="scale">The scaling factor for the box.</param>
    /// <returns></returns>
    Rectangle Box(float i, float amount, int scale) =>
        new(
            0,
            (int)(i / (AudioSegment.Length / scale) * GraphicsDevice.Height()),
            (int)(config.FrequencyWidth * amount * GraphicsDevice.Width()),
            (int)(1f / (AudioSegment.Length / scale) * GraphicsDevice.Height())
        );
}
