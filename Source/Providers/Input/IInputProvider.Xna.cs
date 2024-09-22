// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Input;

partial interface IInputProvider
{
    /// <summary>Provides audio implementation with XNA.</summary>
    // ReSharper disable once ArrangeTypeMemberModifiers
    private sealed partial class Xna : IInputProvider
    {
        /// <summary>The list of binds.</summary>
        readonly List<KeyValuePair<Columns, Input>> _keys = [];

        /// <inheritdoc />
        public Columns Poll()
        {
            var state = Columns.None;
            var mouse = Mouse.GetState();
            var gamepads = GamePads.Four;
            var keyboard = Keyboard.GetState();

            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var (columns, input) in _keys)
                if (input.IsButton && gamepads.IsButtonDown(input.Button) ||
                    input.IsMouse && mouse.ToMouseButtons().Has(input.Mouse) ||
                    input.IsKey && keyboard.IsKeyDown(input.Key))
                    state |= columns;

            return state;
        }

        /// <inheritdoc />
        public bool Add(Columns key, ReadOnlySpan<char> value)
        {
            if (value.TryInto<Input>() is not { } input)
                return false;

            _keys.Add(new(key, input));
            return true;
        }

        /// <inheritdoc />
        void IDisposable.Dispose() { }
    }
}
