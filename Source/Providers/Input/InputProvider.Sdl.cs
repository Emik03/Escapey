// SPDX-License-Identifier: MPL-2.0
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
            value.TryInto<Input>() is { } input && _keys.AndAdd(new(key, input)) is var _;

        /// <inheritdoc />
        public override string GetValidValues() => Input.GetValidValues().Conjoin();

        /// <inheritdoc />
        public override Columns Poll()
        {
            var column = Columns.None;
            var mouse = Mouse.GetState();
            var gamepads = GamePads.Four;
            var keyboard = Keyboard.GetState();

            foreach (var (columns, input) in _keys.AsSpan())
                if (input.IsButton && gamepads.IsButtonDown(input.Button) ||
                    input.IsMouse && mouse.ToMouseButtons().Has(input.Mouse) ||
                    input.IsKey && keyboard.IsKeyDown(input.Key))
                    column |= columns;

            return column;
        }
    }
}
