// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Input;

partial class InputProvider
{
    sealed partial class Sdl
    {
        /// <summary>Represents some input.</summary>
        [Choice.Button<Buttons>.Mouse<MouseButtons>.Key<Keys>]
        readonly partial struct Input : ISpanParsable<Input>
        {
            /// <summary>The prefix for button types.</summary>
            const string ButtonType = "Button.";

            /// <summary>The prefix for mouse types.</summary>
            const string MouseType = "Mouse.";

            /// <summary>The prefix for key types.</summary>
            const string KeyType = "Key.";

            /// <inheritdoc />
            public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Input result) =>
                TryParse(s.AsSpan(), provider, out result);

            /// <inheritdoc />
            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Input result) =>
                s.Trim() is var t && t.StartsWith(ButtonType, StringComparison.OrdinalIgnoreCase) ?
                    (result = t[ButtonType.Length..].IntoEnum<Buttons>()) != default(Buttons) :
                    t.StartsWith(MouseType, StringComparison.OrdinalIgnoreCase) ?
                        (result = t[MouseType.Length..].IntoEnum<MouseButtons>()) != default(MouseButtons) :
                        (result = (t.StartsWith(KeyType, StringComparison.OrdinalIgnoreCase) ? t[KeyType.Length..] : t)
                           .IntoEnum<Keys>()) !=
                        default(Keys);

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
