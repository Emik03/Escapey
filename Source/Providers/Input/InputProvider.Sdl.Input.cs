// SPDX-License-Identifier: LicenseRef-PolyForm-Strict
namespace Escapey.Providers.Input;

partial class InputProvider
{
    sealed partial class Sdl
    {
        /// <summary>Represents some input.</summary>
        [Choice.Button<Buttons>.Mouse<MouseButtons>.Key<Keys>, Union]
        readonly partial struct Input : ISpanParsable<Input>
        {
            const StringComparison O = StringComparison.OrdinalIgnoreCase;

            /// <summary>The prefix for button types.</summary>
            const string ButtonType = $"{nameof(Button)}.";

            /// <summary>The prefix for mouse types.</summary>
            const string MouseType = $"{nameof(Mouse)}.";

            /// <summary>The prefix for key types.</summary>
            const string KeyType = $"{nameof(Key)}.";

            /// <inheritdoc />
            public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Input result) =>
                TryParse(s.AsSpan(), provider, out result);

            /// <inheritdoc />
            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Input result) =>
                s.Trim() is var t && (result = default) is var _ && t.StartsWith(ButtonType, O) ?
                    Enum.TryParse(t[ButtonType.Length..], true, out Buttons b) && (result = b) is var _ :
                    t.StartsWith(MouseType, O) ?
                        Enum.TryParse(t[MouseType.Length..], true, out MouseButtons m) && (result = m) is var _ :
                        (t.StartsWith(KeyType, O) ? t[KeyType.Length..] : t) is var k &&
                        Enum.TryParse(k, true, out Keys key) ? (result = key) is var _ : (result = default) is var _;

            /// <summary>Gets all valid values.</summary>
            /// <returns>All valid values.</returns>
            public static ImmutableArray<string> GetValidValues() =>
            [
                ..Enum.GetValues<Buttons>().Select(x => $"{ButtonType}{x}"),
                ..Enum.GetValues<MouseButtons>().Select(x => $"{MouseType}{x}"),
                ..Enum.GetValues<Keys>().Select(x => $"{KeyType}{x}"),
            ];

            /// <inheritdoc />
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static Input Parse(string s, IFormatProvider? provider) =>
                TryParse(s, provider, out var input) ? input : throw new FormatException(s);

            /// <inheritdoc />
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static Input Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
                TryParse(s, provider, out var input) ? input : throw new FormatException(s.ToString());
        }
    }
}
