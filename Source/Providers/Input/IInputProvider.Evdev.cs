// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Input;

using static ButtonState;

partial interface IInputProvider
{
    /// <summary>Provides audio implementation with evdev.</summary>
    // ReSharper disable once ArrangeTypeMemberModifiers
    private sealed partial class Evdev : IInputProvider
    {
        /// <summary>The upper limit of <see cref="KeyEventCodes"/></summary>
        const int KeyCount = 0x300;

        /// <summary>The prefix for keyboard key codes.</summary>
        const string KeyPrefix = "KEY_";

        /// <summary>Contains the shortened aliases for <see cref="KeyEventCodes"/>.</summary>
        static readonly ImmutableArray<(KeyEventCodes Code, string Alias)> s_aliases =
        [
            ..Enum
               .GetValues<KeyEventCodes>()
               .Select(x => (Code: x, Alias: $"{x}"))
               .Where(x => x.Alias.StartsWith(KeyPrefix))
               .Select(x => x with { Alias = x.Alias[KeyPrefix.Length..] }),
        ];

        /// <summary>Contains the state of the keys.</summary>
        readonly bool[] _keyState = new bool[KeyCount];

        /// <summary>Contains the state of the keys.</summary>
        readonly bool[,] _keys = new bool[sizeof(Columns) * BitsInByte, KeyCount];

        /// <summary>Contains the current input.</summary>
        Columns _col;

        /// <summary>Contains every device that can be polled.</summary>
        ImmutableArray<Device> _devices;

        /// <summary>Initializes a new instance of the <see cref="IInputProvider.Evdev"/> class.</summary>
        /// <param name="warnings">The warnings.</param>
        public Evdev(out ImmutableArray<Exception> warnings)
        {
            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
            {
                warnings = [new PlatformNotSupportedException("'evdev' is only supported on Linux and FreeBSD.")];
                return;
            }

            var accumulator = ImmutableArray.CreateBuilder<Exception>();

            if (!Environment.IsPrivilegedProcess)
                accumulator.Add(new WarningException("Evdev is not privileged, may not work on some input devices."));

            if (Go(() => Directory.EnumerateFiles("/dev/input", "event*"), out var e, out var paths))
            {
                accumulator.Add(e);
                _devices = [];
                warnings = accumulator.DrainToImmutable();
                return;
            }

            var devices = ImmutableArray.CreateBuilder<Device>();

            foreach (var device in paths)
                if (Device.TryOpen(device, out var dev, out e))
                    devices.Add(dev);
                else
                    accumulator.Add(e);

            _devices = devices.DrainToImmutable();
            warnings = accumulator.DrainToImmutable();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var device in _devices)
                device.Dispose();

            _devices = [];
        }

        /// <inheritdoc />
        public bool Add<TSeparator, TStrategy>(Columns key, scoped SplitSpan<char, TSeparator, TStrategy> values)
        {
            Span2D<bool> keys = _keys;
            var flag = true;

            foreach (var value in values)
                _ = TryParse(value) is { } code ? keys[key.ToIndex(), (int)code] = true : flag = false;

            return flag;
        }

        /// <inheritdoc />
        public override string ToString() => $"[{_devices.Conjoin()}]";

        /// <inheritdoc />
        // ReSharper disable once CognitiveComplexity
        public Columns Poll()
        {
            Span2D<bool> keys = _keys;

            foreach (var device in _devices)
                while (device.Next() is var (state, code))
                    if (state is not null && (_keyState[code] = state is Pressed) is var _)
                        for (var i = 0; i < keys.Height; i++)
                            if (keys[i, code])
                                _col = state is Pressed ? _col | i.ToColumns() : _col & ~i.ToColumns();

            return _col;
        }

        /// <summary>Tries to parse the <see cref="KeyEventCodes"/> from <paramref name="value"/>.</summary>
        /// <param name="value">The <see cref="KeyEventCodes"/> to parse.</param>
        /// <returns>The parsed <see cref="KeyEventCodes"/> if successful; otherwise, <see langword="null"/>.</returns>
        static KeyEventCodes? TryParse(scoped ReadOnlySpan<char> value)
        {
            foreach (var (code, alias) in s_aliases)
                if (alias.EqualsIgnoreCase(value))
                    return code;

            return value.TryIntoEnum<KeyEventCodes>();
        }
    }
}
