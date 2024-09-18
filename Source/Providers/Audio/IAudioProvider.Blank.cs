// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Audio;

partial interface IAudioProvider
{
    /// <summary>The blank audio provider.</summary>
    /// <param name="real">The real buffer.</param>
    private sealed class Blank(float[] real) : IAudioProvider
    {
        /// <inheritdoc />
        public float[] Imaginary { get; } = new float[Length];

        /// <inheritdoc />
        public float[] Real =>
            real.Length is Length
                ? real
                : throw new ArgumentOutOfRangeException(nameof(real), real.Length, $"Must be {Length} elements long.");

        /// <inheritdoc />
        public AudioSegment Segment { get; } = new();

        /// <inheritdoc />
        void IDisposable.Dispose() { }

        /// <inheritdoc />
        [MustUseReturnValue]
        public AudioSegment Poll()
        {
            FFT(this);
            return Segment;
        }

        /// <inheritdoc />
        [MustUseReturnValue, Pure]
        public ReadOnlySpan<float> PollRaw() => real;
    }
}
