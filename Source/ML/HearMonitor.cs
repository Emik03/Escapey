// SPDX-License-Identifier: MPL-2.0
namespace Escapey.ML;

/// <summary>Encapsulates the model for predicting phonemes.</summary>
/// <param name="game">The game that this component will belong to.</param>
/// <param name="ml">The machine learning context.</param>
/// <param name="transformer">The transformer for the model.</param>
/// <param name="config">The configuration.</param>
sealed class HearMonitor(
    Letterboxed2DGame game,
    MLContext? ml,
    [HandlesResourceDisposal] ITransformer? transformer,
    Config config
)
    : DrawableGameComponent(game)
{
    /// <summary>The number of occurrences of each mouth state.</summary>
    readonly int[] _count = new int[Config.Mouths.Length];

    /// <summary>The prediction engine.</summary>
    readonly PredictionEngine<AudioSegment, Prediction>? _engine =
        ml?.Model.CreatePredictionEngine<AudioSegment, Prediction>(transformer);

    /// <summary>The font to draw with.</summary>
    readonly SpriteFont _font = game.Content.Load<SpriteFont>("Fonts/main");

    /// <summary>The previous mouth states.</summary>
    Sprite.Mouth[] _order = [];

    /// <summary>The current prediction.</summary>
    Prediction _prediction = new();

    /// <summary>The audio segment to predict.</summary>
    AudioSegment _segment = new();

    /// <summary>Creates the new <see cref="HearMonitor"/>.</summary>
    /// <param name="game">The game that this component will belong to.</param>
    /// <param name="config">The configuration.</param>
    /// <returns>The new <see cref="HearMonitor"/>.</returns>
    [MustDisposeResource]
    public static HearMonitor From(Letterboxed2DGame game, Config config)
    {
        var init = bool.TryParse(Environment.GetEnvironmentVariable("ESCAPEY_INIT"), out var i) && i;
        var file = Path.Join(Config.Folder, config.Profile);
        var modelFile = Path.ChangeExtension(file, "mlnet");
        var dataFile = Path.ChangeExtension(file, "dat");
        MLContext ml = new();

        switch (init)
        {
            case false when File.Exists(modelFile): return new(game, ml, ml.Model.Load(modelFile, out _), config);
            case false when !config.Audio.HasData: return new(game, null, null, config);
        }

        var trainer = ml.MulticlassClassification.Trainers.OneVersusAll(
            ml.BinaryClassification.Trainers.LbfgsLogisticRegression()
        );

        var data = LoadOrSaveData(ml, config, dataFile, init);
        string[] features = [..AudioSegment.Length.For(x => $"E{x}"), nameof(AudioSegment.NormalizationFactor)];
        Console.WriteLine("Hear Monitor will now train on your data. This may take a while, so please be patient!");
#pragma warning disable IDISP001
        var transformer = ml.Transforms
#pragma warning restore IDISP001
           .ReplaceMissingValues(features.ConvertAll(x => new InputOutputColumnPair(x)))
           .Append(ml.Transforms.Concatenate("Features", features))
           .Append(ml.Transforms.Conversion.MapValueToKey("Label", maximumNumberOfKeys: Config.Mouths.Length))
           .Append(trainer)
           .Append(ml.Transforms.Conversion.MapKeyToValue("PredictedLabel"))
           .Fit(data);

        ml.Model.Save(transformer, data.Schema, modelFile);
        var evaluate = ml.BinaryClassification.Evaluate(data);
        Console.WriteLine($"Accuracy: {evaluate.Accuracy:p2}, Loss: {evaluate.LogLoss}.");
        return new(game, ml, transformer, config);
    }

    /// <summary>Polls for the current mouth.</summary>
    /// <returns>The current mouth.</returns>
    public Sprite.Mouth Poll()
    {
        const Sprite.Mouth Neutral = Sprite.Mouth.Upset;

        if (_engine is null)
            return Neutral;

        if (config.Stabilize is var stabilize && _order.Length != stabilize)
        {
            _order = new Sprite.Mouth[stabilize];
            _order.AsSpan().Fill(Neutral);
            _count.AsSpan(1).Clear();
            _count[0] = stabilize;
        }

        ref var count = ref MemoryMarshal.GetArrayDataReference(_count);

        if (config.Audio.Poll() is { } segment)
        {
            var last = _order.Length - 1;
            _engine.Predict(_segment = segment, ref _prediction);
            ref var order = ref MemoryMarshal.GetArrayDataReference(_order);
            Unsafe.Add(ref count, order - Neutral)--;
            MemoryMarshal.CreateReadOnlySpan(Unsafe.Add(ref order, 1), last).CopyTo(_order);
            Unsafe.Add(ref count, (Unsafe.Add(ref order, last) = _prediction.Mouth) - Neutral)++;
        }

        ref readonly var max = ref count;
        ref var step = ref Unsafe.Add(ref count, 1);
        ref readonly var end = ref Unsafe.Add(ref count, _count.Length);

        for (; Unsafe.IsAddressLessThan(step, end); step = ref Unsafe.Add(ref step, 1))
            if (max < step)
                max = ref step;

        return (Sprite.Mouth)(Unsafe.ByteOffset(count, max) / sizeof(int) + (nint)Neutral);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _engine?.Dispose();

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override void Draw(GameTime gameTime)
    {
        if (!Visible || config.FrequencyGraph.Result.A is 0 || config.FrequencyWidth is 0)
            return;

        var color = config.FrequencyGraph;
        var scale = config.FrequencyScale;
        ref var start = ref _segment.Head;
        ref var end = ref Unsafe.Add(ref start, AudioSegment.Length / scale);

        for (var i = 0f; Unsafe.IsAddressLessThan(ref start, ref end); start = ref Unsafe.Add(ref start, 1), i++)
            game.Batch.Draw(game.WhitePixel, Box(i, start * _segment.NormalizationFactor.Sqrt(), scale), color);

        for (var i = 0; i < _count.Length; i++)
            game.Batch.Draw(game.WhitePixel, Box(i, _count[i], null), color);

        Vector2 position = new((GraphicsDevice.Width() - 72) / 2f, GraphicsDevice.Height() - 108);
        game.Batch.DrawString(_font, _prediction.ToString(), position, color);
    }

    /// <summary>Loads or saves the data.</summary>
    /// <param name="ml">The machine learning context.</param>
    /// <param name="config">The configuration.</param>
    /// <param name="file">The path to the data to either save or load.</param>
    /// <param name="init">Whether to always start from scratch, not reading from disk.</param>
    /// <returns>The <see cref="IDataView"/> containing the training date.</returns>
    static IDataView LoadOrSaveData(MLContext ml, Config config, string file, bool init)
    {
        if (!init && File.Exists(file))
            return ml.Data.LoadFromBinary(file);

        var data = ml.Data.LoadFromEnumerable(config.Capture());
        using var stream = File.Open(file, FileMode.OpenOrCreate);
        ml.Data.SaveAsBinary(data, stream);
        return data;
    }

    /// <summary>Creates the box for the frequency graph.</summary>
    /// <param name="i">The index of the box.</param>
    /// <param name="amount">The length of the box.</param>
    /// <param name="scale">The scaling factor for the box.</param>
    /// <returns>The box where height represents pitch and width represents amplitude.</returns>
    Rectangle Box(float i, float amount, int? scale)
    {
        var max = scale is { } s ? AudioSegment.Length / s : _count.Length;

        var width = (int)(amount *
            GraphicsDevice.Width() *
            (scale is null ? config.OrderWidth / _order.Length : config.FrequencyWidth));

        return new(
            scale is null ? GraphicsDevice.Width() - width : 0,
            (int)(i / max * GraphicsDevice.Height()),
            width,
            (int)(1f / max * GraphicsDevice.Height())
        );
    }
}
