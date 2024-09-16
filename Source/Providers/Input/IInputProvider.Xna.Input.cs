// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Input;

partial interface IInputProvider
{
    sealed partial class Xna : IInputProvider
    {
        /// <summary>Represents some input.</summary>
        [Choice.Button<Buttons>.Mouse<MouseButtons>.Key<Keys>]
        readonly partial struct Input : ISpanParsable<Input>
        {
            /// <summary>The prefix for button types.</summary>
            const string ButtonType = "button.";

            /// <summary>The prefix for key types.</summary>
            const string KeyType = "key.";

            /// <summary>The prefix for mouse types.</summary>
            const string MouseType = "mouse.";

            /// <inheritdoc />
            public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Input result) =>
                TryParse(s.AsSpan(), provider, out result);

            /// <inheritdoc />
            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Input result) =>
                s.Trim() is var t && t.StartsWith(ButtonType) ?
                    (result = t[ButtonType.Length..].IntoEnum<Buttons>()) != default(Buttons) :
                    t.StartsWith(MouseType) ?
                        (result = t[MouseType.Length..].IntoEnum<MouseButtons>()) != default(MouseButtons) :
                        (result = (t.StartsWith(KeyType) ? t[KeyType.Length..] : t).IntoEnum<Keys>()) != default(Keys);

            /// <inheritdoc />
            public static Input Parse(string s, IFormatProvider? provider) =>
                TryParse(s, provider, out var input) ? input : throw new FormatException(s);

            /// <inheritdoc />
            public static Input Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
                TryParse(s, provider, out var input) ? input : throw new FormatException(s.ToString());
        }
    }
}
