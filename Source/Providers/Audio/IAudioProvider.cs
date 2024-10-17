// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Audio;

/// <summary>Represents the provider for polling audio.</summary>
partial interface IAudioProvider : IDisposable
{
    /// <summary>The length of the audio buffer for training.</summary>
    const int Length = 512;

    /// <summary>The Bluestein transform.</summary>
    static (ImmutableArray<float> Real, ImmutableArray<float> Imaginary) Bluestein { get; } = Length.Bluestein<float>();

    /// <summary>Gets the imaginary part of the sample vector.</summary>
    float[] Imaginary { get; }

    /// <summary>Gets the real part of the sample vector.</summary>
    float[] Real { get; }

    /// <summary>Gets the latest audio buffer.</summary>
    AudioSegment Segment { get; }

    /// <summary>Writes the Fast Fourier Transform to the <see cref="AudioSegment"/>.</summary>
    /// <param name="that">The instance to mutate.</param>
    static void FFT<T>(T that)
        where T : IAudioProvider
    {
        // This implementation is a bit evil, but is about as fast as it gets.
        // To say I've optimized this algorithm is a bit of an understatement.
        var (real, imaginary, segment) = (that.Real, that.Imaginary, that.Segment);
        Debug.Assert(real.All(x => x is <= 1 and >= -1));
        Debug.Assert(imaginary.All(x => x is 0));
        Debug.Assert(imaginary.Length >= Length);
        Debug.Assert(real.Length >= Length);

        Bluestein.FFT(real, imaginary);
        var max = MaxHypot(that);

        // Ensures in range of [0, 1]. It is possible to exceed 2 by shouting. We choose to clamp the value since it's
        // far more important to distinguish the difference between quiet and loud rather than loud and even louder.
        // Sounds like /f/ will sound very similar to silence, with a subtle volume difference being the largest factor.
        segment.NormalizationFactor = max.Min(1);

        ref var current = ref segment.Head;
        ref readonly var end = ref Unsafe.Add(ref current, AudioSegment.Length);

        ref readonly var endVector = ref Unsafe.Add(
            ref current,
            Vector<float>.IsSupported && Vector.IsHardwareAccelerated
                ? AudioSegment.Length - Vector<float>.Count + 1
                : 0
        );

        for (; Unsafe.IsAddressLessThan(current, endVector); current = ref Unsafe.Add(ref current, Vector<float>.Count))
            (Vector.LoadUnsafe(current) / max).StoreUnsafe(ref current);

        for (; Unsafe.IsAddressLessThan(current, end); current = ref Unsafe.Add(ref current, 1))
            current /= max;

        imaginary.AsSpan().Clear();
        segment.AssertNormalized();
    }

    /// <summary>Writes the hypotenuse to <see cref="Segment"/> while finding the maximum.</summary>
    /// <param name="that">The instance to mutate.</param>
    /// <returns>The maximum hypotenuse.</returns>
    [MustUseReturnValue]
    static float MaxHypot<T>(T that)
        where T : IAudioProvider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void StoreUnsafe(in float real, in float imaginary, ref float segment, ref Vector<float> maxVector)
        {
            var hypot = Vector.Hypot(Vector.LoadUnsafe(real), Vector.LoadUnsafe(imaginary));
            maxVector = Vector.Max(maxVector, hypot);
            hypot.StoreUnsafe(ref segment);
        }

        var (realBuffer, imaginaryBuffer, segmentBuffer) = (that.Real, that.Imaginary, that.Segment);

        var max = float.Epsilon;
        ref var segment = ref segmentBuffer.Head;
        ref var real = ref MemoryMarshal.GetArrayDataReference(realBuffer);
        ref var imaginary = ref MemoryMarshal.GetArrayDataReference(imaginaryBuffer);
        Debug.Assert(realBuffer.Length >= AudioSegment.Length && imaginaryBuffer.Length >= AudioSegment.Length);

        if (!Vector<float>.IsSupported || !Vector.IsHardwareAccelerated || Vector<float>.Count > AudioSegment.Length)
        {
            ref readonly var end = ref Unsafe.Add(ref segment, AudioSegment.Length);

            while (Unsafe.IsAddressLessThan(segment, end))
            {
                max = max.Max(segment = float.Hypot(imaginary, real));

                real = ref Unsafe.Add(ref real, 1);
                segment = ref Unsafe.Add(ref segment, 1);
                imaginary = ref Unsafe.Add(ref imaginary, 1);
            }

            return max;
        }

        ref var segmentLast = ref Unsafe.Add(ref segment, AudioSegment.Length - Vector<float>.Count);
        ref readonly var realLast = ref Unsafe.Add(ref real, AudioSegment.Length - Vector<float>.Count);
        ref readonly var imaginaryLast = ref Unsafe.Add(ref imaginary, AudioSegment.Length - Vector<float>.Count);

        var maxVector = Vector<float>.Zero;
        StoreUnsafe(real, imaginary, ref segment, ref maxVector);

        real = ref Unsafe.Add(ref real, Vector<float>.Count);
        segment = ref Unsafe.Add(ref segment, Vector<float>.Count);
        imaginary = ref Unsafe.Add(ref imaginary, Vector<float>.Count);

        while (Unsafe.IsAddressLessThan(segment, segmentLast))
        {
            StoreUnsafe(real, imaginary, ref segment, ref maxVector);

            real = ref Unsafe.Add(ref real, Vector<float>.Count);
            segment = ref Unsafe.Add(ref segment, Vector<float>.Count);
            imaginary = ref Unsafe.Add(ref imaginary, Vector<float>.Count);
        }

        StoreUnsafe(realLast, imaginaryLast, ref segmentLast, ref maxVector);

        for (var index = 0; index < Vector<float>.Count; index++)
            max = max.Max(maxVector[index]);

        return max;
    }

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
    /// <returns>
    /// The raw PCM buffer from the audio provider, or empty if no new data.
    /// <see cref="Real"/> may or may not be mutated, and therefore may not be equivalent to the return value.
    /// </returns>
    [MustUseReturnValue]
    ReadOnlySpan<float> PollRaw();

    /// <summary>Polls the audio provider.</summary>
    /// <returns>
    /// The raw PCM buffer from the audio provider, blocking until the next data is available.
    /// <see cref="Real"/> may or may not be mutated, and therefore may not be equivalent to the return value.
    /// </returns>
    [MustUseReturnValue]
    ReadOnlySpan<float> WaitForRaw()
    {
        ReadOnlySpan<float> raw;

        while ((raw = PollRaw()).IsEmpty) { }

        return raw;
    }
}
