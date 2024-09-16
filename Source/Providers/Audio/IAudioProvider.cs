// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Audio;

using F32Vec = System.Numerics.Vector<float>;

/// <summary>Represents the provider for polling audio.</summary>
partial interface IAudioProvider : IDisposable
{
    /// <summary>The length of the audio buffer for training.</summary>
    const int Length = 200;

    /// <summary>Gets the imaginary part of the sample vector.</summary>
    float[] Imaginary { get; }

    /// <summary>Gets the real part of the sample vector.</summary>
    float[] Real { get; }

    /// <summary>Gets the latest audio buffer.</summary>
    AudioSegment Segment { get; }

    /// <summary>Creates a default audio provider.</summary>
    /// <returns>The default <see cref="IAudioProvider"/>.</returns>
    [MustDisposeResource]
    static IAudioProvider Default() =>
        PipeWire.Instance(out var warnings) is var pipewire && warnings is [] ? pipewire : new Alc();

    /// <summary>Creates an input provider from an alias.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="warnings">The warnings.</param>
    /// <returns>The <see cref="IInputProvider"/> from the parameter <paramref name="alias"/>.</returns>
    [MustDisposeResource]
    static IAudioProvider FromAlias(scoped ReadOnlySpan<char> alias, out ImmutableArray<Exception> warnings)
    {
        if (alias.EqualsIgnoreCase(nameof(PipeWire)))
            return PipeWire.Instance(out warnings);

        warnings = alias.IsWhiteSpace() || alias.EqualsIgnoreCase(nameof(Alc))
            ? []
            : [new FormatException($"Unrecognized alias, falling back to ALC: {alias}")];

        return new Alc();
    }

    /// <summary>Polls the audio provider.</summary>
    /// <returns>
    /// The value <see langword="true"/> if new audio was captured, which can be
    /// read from <see cref="Segment"/>; otherwise, <see langword="false"/>.
    /// </returns>
    bool Poll();

    /// <summary>Writes the Fast Fourier Transform to the <see cref="AudioSegment"/>.</summary>
    /// <param name="that">The instance to mutate.</param>
    public static void FFT<T>(T that)
        where T : IAudioProvider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int VectorOffset() => F32Vec.IsSupported && Vector.IsHardwareAccelerated ? Length - F32Vec.Count + 1 : 0;

        // This implementation is a bit evil, but is about as fast as it gets.
        // To say I've optimized this algorithm is a bit of an understatement.
        var (real, imaginary, segment) = (that.Real, that.Imaginary, that.Segment);
        Debug.Assert(real.All(x => x is <= 1 and >= -1));
        Debug.Assert(imaginary.All(x => x is 0));
        Debug.Assert(imaginary.Length >= Length);
        Debug.Assert(real.Length >= Length);
        Fourier.Forward(real, imaginary);

        var max = MaxHypot(that);
        ref var current = ref segment.Head;
        ref readonly var end = ref Unsafe.Add(ref current, Length);
        ref readonly var endVector = ref Unsafe.Add(ref current, VectorOffset());

        // Ensures in range of [0, 1]. It is possible to exceed 2 by shouting. We choose to clamp the value since it's
        // far more important to distinguish the difference between quiet and loud rather than loud and even louder.
        // Sounds like /f/ will sound very similar to silence, with a subtle volume difference being the largest factor.
        segment.NormalizationFactor = max.Min(1);

        while (Unsafe.IsAddressLessThan(current, endVector))
        {
            Vector.Divide(Vector.LoadUnsafe(current), max).StoreUnsafe(ref current);
            current = ref Unsafe.Add(ref current, F32Vec.Count);
        }

        while (Unsafe.IsAddressLessThan(current, end))
        {
            current /= max;
            current = ref Unsafe.Add(ref current, 1);
        }

        imaginary.AsSpan().Clear();
        segment.AssertNormalized();
    }

    /// <summary>Writes the hypotenuse to <see cref="Segment"/> while finding the maximum.</summary>
    /// <param name="that">The instance to mutate.</param>
    /// <returns>The maximum hypotenuse.</returns>
    public static float MaxHypot<T>(T that)
        where T : IAudioProvider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void StoreUnsafe(in float real, in float imaginary, ref float segment, ref F32Vec maxVector)
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
        Debug.Assert(realBuffer.Length >= Length && imaginaryBuffer.Length >= Length);

        if (!F32Vec.IsSupported || !Vector.IsHardwareAccelerated || F32Vec.Count > Length)
        {
            ref readonly var end = ref Unsafe.Add(ref segment, Length);

            while (Unsafe.IsAddressLessThan(segment, end))
            {
                max = max.Max(segment = float.Hypot(imaginary, real));

                real = ref Unsafe.Add(ref real, 1);
                segment = ref Unsafe.Add(ref segment, 1);
                imaginary = ref Unsafe.Add(ref imaginary, 1);
            }

            return max;
        }

        ref var segmentLast = ref Unsafe.Add(ref segment, Length - F32Vec.Count);
        ref readonly var realLast = ref Unsafe.Add(ref real, Length - F32Vec.Count);
        ref readonly var imaginaryLast = ref Unsafe.Add(ref imaginary, Length - F32Vec.Count);

        var maxVector = F32Vec.Zero;
        StoreUnsafe(real, imaginary, ref segment, ref maxVector);

        real = ref Unsafe.Add(ref real, F32Vec.Count);
        segment = ref Unsafe.Add(ref segment, F32Vec.Count);
        imaginary = ref Unsafe.Add(ref imaginary, F32Vec.Count);

        while (Unsafe.IsAddressLessThan(segment, segmentLast))
        {
            StoreUnsafe(real, imaginary, ref segment, ref maxVector);

            real = ref Unsafe.Add(ref real, F32Vec.Count);
            segment = ref Unsafe.Add(ref segment, F32Vec.Count);
            imaginary = ref Unsafe.Add(ref imaginary, F32Vec.Count);
        }

        StoreUnsafe(realLast, imaginaryLast, ref segmentLast, ref maxVector);

        for (var index = 0; index < F32Vec.Count; index++)
            max = max.Max(maxVector[index]);

        return max;
    }
}
