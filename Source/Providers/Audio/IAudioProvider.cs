// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Audio;

/// <summary>Represents the provider for polling audio.</summary>
partial interface IAudioProvider : IDisposable
{
    /// <summary>The length of the audio buffer for training.</summary>
    const int Length = 400;

    /// <summary>Gets the preferred microphone name.</summary>
    static string? PreferredMicrophone { get; } = Environment.GetEnvironmentVariable("ESCAPEY_MICROPHONE");

    /// <summary>Gets the latest audio buffer.</summary>
    AudioSegment Segment { get; }

    /// <summary>Creates the blank audio provider around the given <see cref="float"/> <see cref="Array"/>.</summary>
    /// <param name="real">The <see cref="float"/> <see cref="Array"/> to use.</param>
    /// <returns>The <see cref="IAudioProvider"/> that does not poll audio.</returns>
    [MustDisposeResource, Pure]
    static IAudioProvider CreateBlank(float[] real) => new Blank(real);

    /// <summary>Creates a default audio provider.</summary>
    /// <returns>The default <see cref="IAudioProvider"/>.</returns>
    [MustDisposeResource, MustUseReturnValue]
    static IAudioProvider Default() =>
        PipeWire.Instance(out var warnings) is var pipewire && warnings is [] ? pipewire : new Alc();

    /// <summary>Creates an input provider from an alias.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="warnings">The warnings.</param>
    /// <returns>The <see cref="IInputProvider"/> from the parameter <paramref name="alias"/>.</returns>
    [MustDisposeResource, MustUseReturnValue]
#pragma warning disable IDISP015 // Normally a code smell, but PipeWire.Dispose is a no-op.
    static IAudioProvider FromAlias(scoped ReadOnlySpan<char> alias, out ImmutableArray<Exception> warnings)
#pragma warning restore IDISP015
    {
        if (alias.EqualsIgnoreCase(nameof(Blank)) && (warnings = []) is var _)
            return new Blank(new float[Length]);

        if (alias.EqualsIgnoreCase(nameof(PipeWire)))
            return PipeWire.Instance(out warnings);

        warnings = alias.IsWhiteSpace() || alias.EqualsIgnoreCase(nameof(Alc))
            ? []
            : [new FormatException($"Unrecognized alias, falling back to ALC: {alias}")];

        return new Alc();
    }

    /// <summary>Polls the audio provider.</summary>
    /// <returns>
    /// The mutated <see cref="AudioSegment"/> containing the current transformed data,
    /// or <see langword="null"/> if the next data is not ready.
    /// </returns>
    [MustUseReturnValue]
    AudioSegment? Poll();

    /// <summary>Polls the audio provider.</summary>
    /// <returns>The raw PCM buffer from the audio provider, or empty if no new data.</returns>
    [MustUseReturnValue]
    Span<float> PollRaw();

    /// <summary>Polls the audio provider.</summary>
    /// <returns>The raw PCM buffer from the audio provider, blocking until the next data is available.</returns>
    [MustUseReturnValue]
    Span<float> WaitForRaw()
    {
        Span<float> raw;

        while ((raw = PollRaw()).IsEmpty) { }

        return raw;
    }
}
