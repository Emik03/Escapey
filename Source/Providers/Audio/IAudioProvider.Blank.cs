// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Audio;

partial interface IAudioProvider
{
    /// <summary>The blank audio provider.</summary>
    /// <param name="real">The real buffer.</param>
    private sealed class Blank(float[] real) : IAudioProvider
    {
        /// <inheritdoc />
        public AudioSegment Segment { get; } = new();

        /// <inheritdoc />
        void IDisposable.Dispose() { }

        /// <inheritdoc />
        [MustUseReturnValue]
        public AudioSegment Poll()
        {
            Segment.Forward(real);
            return Segment;
        }

        /// <inheritdoc />
        [MustUseReturnValue, Pure]
        public Span<float> PollRaw() => real;
    }
}
