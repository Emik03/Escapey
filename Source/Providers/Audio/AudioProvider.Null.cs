// SPDX-License-Identifier: LicenseRef-PolyForm-Strict
namespace Escapey.Providers.Audio;

partial class AudioProvider
{
    /// <summary>The blank audio provider.</summary>
    /// <param name="real">The real buffer.</param>
    sealed class Null(float[]? real) : AudioProvider
    {
        float[]? _real = real;

        [MemberNotNullWhen(true, nameof(_real))]
        bool IsRenting { get; set; }

        public Null()
            : this(new float[Length]) { }

        /// <inheritdoc />
        public override bool HasData => false;

        /// <inheritdoc />
        public override AudioSegment Segment { get; } = new();

        /// <inheritdoc />
        [MustUseReturnValue]
        public override AudioSegment? Poll()
        {
            if (_real is null)
                return null;

            Segment.Forward(PollRaw());
            return Segment;
        }

        /// <inheritdoc />
        [MustUseReturnValue, Pure]
        public override Span<float> PollRaw()
        {
            Span<float> span = _real;
            return span.UnsafelyTake(span.Length.Min(Length));
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            if (!IsRenting)
                return;

            IsRenting = false;
            ArrayPool<float>.Shared.Return(_real);
            _real = null;
        }
    }
}
