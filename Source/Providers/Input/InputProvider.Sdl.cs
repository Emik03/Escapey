// SPDX-License-Identifier: LicenseRef-PolyForm-Strict
namespace Escapey.Providers.Input;

partial class InputProvider
{
    /// <summary>Provides input processing with Simple DirectMedia Layer.</summary>
    // ReSharper disable once ArrangeTypeMemberModifiers
    sealed partial class Sdl : InputProvider
    {
        /// <summary>The list of binds.</summary>
        readonly List<KeyValuePair<Columns, Input>> _keys = [];

        /// <inheritdoc />
        public override void Clear() => _keys.Clear();

        /// <inheritdoc />
        public override void Dispose() { }

        /// <inheritdoc />
        public override bool Add(Columns key, ReadOnlySpan<char> value) =>
            Input.TryParse(value, CultureInfo.InvariantCulture, out var i) && _keys.AndAdd(new(key, i)) is var _;

        /// <inheritdoc />
        public override string GetValidValues() => Input.GetValidValues().Conjoin();

        /// <inheritdoc />
        public override Columns Poll()
        {
            var ret = Columns.None;
            var mouse = Mouse.GetState();
            var gamepads = GamePads.Four;
            var keyboard = Keyboard.GetState();

            foreach (var (column, input) in _keys.AsSpan())
                ret |= input switch
                {
                    { IsMouse: true, Mouse: var m } => mouse.ToMouseButtons().Has(m),
                    { IsButton: true, Button: var b } => gamepads.IsButtonDown(b),
                    { IsKey: true, Key: var k } => keyboard.IsKeyDown(k),
                    _ => false,
                }
                    ? column
                    : default;

            return ret;
        }
    }
}
