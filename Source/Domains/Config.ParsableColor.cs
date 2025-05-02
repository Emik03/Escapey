// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Domains;

partial class Config
{
    public readonly record struct ParsableColor(Color Result) : ISpanParsable<ParsableColor>
    {
        /// <summary>The comparison to use.</summary>
        const StringComparison Comparison = StringComparison.InvariantCultureIgnoreCase;

        /// <summary>The aliases for <see cref="Microsoft.Xna.Framework.Color"/> instances.</summary>
        static readonly FrozenDictionary<string, Color> s_knownColors =
            typeof(Color)
               .GetProperties(BindingFlags.Public | BindingFlags.Static)
               .Where(x => x.CanRead && x.PropertyType == typeof(Color) && x.GetIndexParameters() is [])
               .SelectMany(IncludeAlternateSpellings)
               .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        /// <summary>Gets the <see cref="Result"/>.</summary>
        /// <param name="color">The color to extract.</param>
        /// <returns>The property <see cref="Result"/>.</returns>
        public static implicit operator Color(ParsableColor color) => color.Result;

        /// <summary>Wraps <see cref="ParsableColor"/> around the <see cref="Color"/>.</summary>
        /// <param name="color">The color to wrap.</param>
        /// <returns>The wrapped <see cref="ParsableColor"/> around the parameter <paramref name="color"/>.</returns>
        public static implicit operator ParsableColor(Color color) => new(color);

        /// <summary>The aliases for <see cref="Microsoft.Xna.Framework.Color"/> instances.</summary>
        static readonly FrozenDictionary<string, Color>.AlternateLookup<ReadOnlySpan<char>> s_lookup =
            s_knownColors.GetAlternateLookup<ReadOnlySpan<char>>();

        /// <inheritdoc />
        public static bool TryParse(
            [NotNullWhen(true)] string? s,
            IFormatProvider? provider,
            out ParsableColor result
        ) =>
            (result = Parse(s)) is var _;

        /// <inheritdoc />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ParsableColor result) =>
            (result = Parse(s)) is var _;

        /// <inheritdoc />
        public static ParsableColor Parse(string s, IFormatProvider? provider) => Parse(s);

        /// <inheritdoc />
        public static ParsableColor Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

        /// <inheritdoc />
        public override string ToString()
        {
            foreach (var (key, color) in s_knownColors)
                if (Result == color)
                    return key;

            return $"{Result.R:x2}{Result.G:x2}{Result.B:x2}";
        }

        /// <summary>Attempts to parse a hex number from the provided inputs.</summary>
        /// <param name="fst">The first character.</param>
        /// <param name="snd">The second character.</param>
        /// <returns>The parsed number if successful; otherwise, <see langword="null"/>.</returns>
        static byte? P(char fst, char snd = '\0') =>
            byte.TryParse(
                [fst, snd is '\0' ? fst : snd],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var ret
            )
                ? ret
                : null;

        /// <summary>Attempts to parse a color from the provided inputs.</summary>
        /// <param name="chars">The characters to parse.</param>
        /// <returns>The parsed color if successful; otherwise, <see langword="null"/>.</returns>
        static Color Parse(ReadOnlySpan<char> chars) =>
            chars switch
            {
                _ when s_lookup.TryGetValue(chars, out var knownColor) => knownColor,
                [var r, var g, var b] when (P(r), P(b), P(g)) is ({ } pr, { } pg, { } pb) => new(pr, pg, pb),
                [var r, var g, var b, var a] when (P(r), P(b), P(g), P(a)) is ({ } pr, { } pg, { } pb, { } pa)
                    => new(pr, pg, pb, pa),
                [var r, var rp, var g, var gp, var b, var bp]
                    when (P(r, rp), P(g, gp), P(b, bp)) is ({ } pr, { } pg, { } pb) => new(pr, pg, pb),
                [var r, var rp, var g, var gp, var b, var bp, var a, var ap]
                    when (P(r, rp), P(g, gp), P(b, bp), P(a, ap)) is ({ } pr, { } pg, { } pb, { } pa)
                    => new(pr, pg, pb, pa),
                _ => Color.Transparent,
            };

        /// <summary>Includes alternate spellings for <see cref="Color.Gray"/> and similar.</summary>
        /// <param name="x">The property to include.</param>
        /// <returns>The included properties.</returns>
        static IEnumerable<KeyValuePair<string, Color>> IncludeAlternateSpellings(PropertyInfo x) =>
            (x.GetValue(null) as Color?).GetValueOrDefault() is var color &&
            x.Name.Contains(nameof(Color.Gray), Comparison)
                ? [new(x.Name, color), new(x.Name.Replace(nameof(Color.Gray), "Grey", Comparison), color)]
                : [new(x.Name, color)];
    }
}
