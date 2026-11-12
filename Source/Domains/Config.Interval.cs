// SPDX-License-Identifier: LicenseRef-PolyForm-Strict
namespace Escapey.Domains;

partial class Config
{
    /// <summary>Represents an interval between 0 and 1, inclusive on both ends.</summary>
    /// <param name="result">The float to clamp.</param>
    public readonly struct Interval(float result) : ISpanParsable<Interval>
    {
        /// <summary>Gets the value.</summary>
        public float Result { get; } = result.Clamp(0, 1);

        /// <summary>Gets the <see cref="Result"/>.</summary>
        /// <param name="interval">The number to extract.</param>
        /// <returns>The property <see cref="Result"/>.</returns>
        public static implicit operator float(Interval interval) => interval.Result;

        /// <summary>Wraps <see cref="Interval"/> around the <see langword="float"/>.</summary>
        /// <param name="interval">The number to wrap.</param>
        /// <returns>The wrapped <see cref="Interval"/> around the parameter <paramref name="interval"/>.</returns>
        public static explicit operator Interval(float interval) => new(interval);

        /// <inheritdoc />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Interval result) =>
            (result = Parse(s.AsSpan(), provider)) is var _;

        /// <inheritdoc />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Interval result) =>
            (result = Parse(s, provider)) is var _;

        /// <inheritdoc />
        public static Interval Parse(string? s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);

        /// <inheritdoc />
        public static Interval Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
            new(float.TryParse(s, provider, out var result) ? result : 0);

        /// <inheritdoc />
        public override string ToString() => Result.ToString(CultureInfo.InvariantCulture);
    }
}
