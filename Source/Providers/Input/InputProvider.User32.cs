// SPDX-License-Identifier: LicenseRef-PolyForm-Strict
namespace Escapey.Providers.Input;

partial class InputProvider
{
    sealed partial class User32 : InputProvider
    {
        /// <summary>Initializes a new instance of the <see cref="InputProvider.User32"/> class.</summary>
        /// <param name="warnings">The warnings.</param>
        public User32(out ImmutableArray<Exception> warnings) =>
            warnings = OperatingSystem.IsWindows()
                ? []
                : [new PlatformNotSupportedException("'user32' is only supported on Windows.")];

        /// <summary>The list of binds.</summary>
        readonly List<KeyValuePair<Columns, WinKeys>> _keys = [];

        /// <inheritdoc />
        public override void Clear() => _keys.AsSpan().Clear();

        /// <inheritdoc />
        public override void Dispose() { }

        /// <inheritdoc />
        public override bool Add(Columns key, scoped ReadOnlySpan<char> value)
        {
            ref var k = ref Unsafe.AsRef(Span.LValue(value.TryIntoEnum<WinKeys>())).DangerousGetValueOrNullReference();
            return !Unsafe.IsNullRef(k) && _keys.AndAdd(new(key, k)) is var _;
        }

        /// <inheritdoc />
        public override string GetValidValues() => Enum.GetValues<WinKeys>().Conjoin();

        /// <inheritdoc />
        public override Columns Poll()
        {
            var ret = Columns.None;

            foreach (var (column, input) in _keys.AsSpan())
                ret |= Poll(input) is 0 ? default : column;

            return ret;
        }

        [LibraryImport("user32.dll", EntryPoint = "GetAsyncKeyState")]
        private static partial short Poll(WinKeys vKey);
    }
}
