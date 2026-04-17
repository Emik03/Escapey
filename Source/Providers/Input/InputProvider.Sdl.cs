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
        public override bool Add(Columns key, ReadOnlySpan<char> value)
        {
            ref var input = ref Unsafe.AsRef(Span.LValue(value.TryInto<Input>())).DangerousGetValueOrNullReference();
            return !Unsafe.IsNullRef(input) && _keys.AndAdd(new(key, input)) is var _;
        }

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
                    null => false,
                    Keys k => keyboard.IsKeyDown(k),
                    Buttons b => gamepads.IsButtonDown(b),
                    MouseButtons m => mouse.ToMouseButtons().Has(m),
                }
                    ? column
                    : default;

            return ret;
        }
    }
}
