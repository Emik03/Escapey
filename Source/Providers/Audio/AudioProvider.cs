// SPDX-License-Identifier: LicenseRef-PolyForm-Strict
namespace Escapey.Providers.Audio;

/// <summary>Represents the provider for polling audio.</summary>
abstract partial class AudioProvider : IDisposable, ISpanParsable<AudioProvider>
{
    /// <summary>The length of the audio buffer for training.</summary>
    public const int Length = 400;

    /// <summary>Gets the preferred microphone name.</summary>
    public static string? PreferredMicrophone { get; } = Environment.GetEnvironmentVariable("ESCAPEY_MICROPHONE");

    /// <summary>Indicates whether audio buffers can be processed.</summary>
    public virtual bool HasData => true;

    /// <summary>Gets the latest audio buffer.</summary>
    public abstract AudioSegment Segment { get; }

    /// <inheritdoc />
    public override string ToString() => GetType().Name;

    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false), MustDisposeResource] out AudioProvider result
    ) =>
        (result = Parse(s.AsSpan(), provider)) is var _;

    /// <inheritdoc />
    public static bool TryParse(
        ReadOnlySpan<char> s,
        IFormatProvider? provider,
        [MaybeNullWhen(false), MustDisposeResource] out AudioProvider result
    ) =>
        (result = Parse(s, provider)) is var _;

    /// <summary>Gets the default provider.</summary>
    /// <returns>The default provider.</returns>
    [Pure]
    public static AudioProvider Default() => new Null(new float[Length]);

    /// <summary>Creates the blank audio provider around the given <see cref="float"/> <see cref="Array"/>.</summary>
    /// <param name="real">The <see cref="float"/> <see cref="Array"/> to use.</param>
    /// <returns>The <see cref="AudioProvider"/> that does not poll audio.</returns>
    [Pure]
    public static AudioProvider FromRaw(float[] real) => new Null(real);

    /// <summary>Creates the temporary audio buffer for performing Fourier Transforms.</summary>
    /// <returns>The <see cref="AudioProvider"/> that does not poll audio.</returns>
    [MustDisposeResource, Pure]
    public static AudioProvider Temporary() => new Null();

    /// <inheritdoc />
    public abstract void Dispose();

    /// <summary>Polls the audio provider.</summary>
    /// <returns>
    /// The mutated <see cref="AudioSegment"/> containing the current transformed data,
    /// or <see langword="null"/> if the next data is not ready.
    /// </returns>
    [MustUseReturnValue]
    public abstract AudioSegment? Poll();

    /// <summary>Polls the audio provider.</summary>
    /// <returns>The raw PCM buffer from the audio provider, or empty if no new data.</returns>
    [MustUseReturnValue]
    public abstract Span<float> PollRaw();

    /// <summary>Polls the audio provider.</summary>
    /// <returns>The raw PCM buffer from the audio provider, blocking until the next data is available.</returns>
    [MustUseReturnValue]
    public Span<float> WaitForRaw()
    {
        Span<float> raw;

        while ((raw = PollRaw()).IsEmpty) { }

        return raw;
    }

    /// <inheritdoc />
    [MustDisposeResource]
    public static AudioProvider Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);

    /// <inheritdoc />
    [MustDisposeResource]
    public static AudioProvider Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (s.EqualsIgnoreCase(nameof(Alc)))
            return new Alc();

        if (!s.EqualsIgnoreCase(nameof(PipeWire)))
            return Default();

        if (PipeWire.Instance(out var warnings) is var pipeWire && warnings.IsEmpty)
            return pipeWire;

        EscapeyGame.Log(warnings);
        return new Null(new float[Length]);
    }
}
