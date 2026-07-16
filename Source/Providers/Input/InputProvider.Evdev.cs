// SPDX-License-Identifier: LicenseRef-PolyForm-Strict
namespace Escapey.Providers.Input;

using static ButtonState;

abstract partial class InputProvider
{
    /// <summary>Provides input processing with evdev.</summary>
    // ReSharper disable once ArrangeTypeMemberModifiers
    sealed partial class Evdev : InputProvider
    {
        /// <summary>The upper limit of <see cref="KeyEventCodes"/></summary>
        const int KeyCount = 0x300;

        /// <summary>The prefix for keyboard key codes.</summary>
        const string KeyPrefix = "KEY_";

        /// <summary>Contains the shortened aliases for <see cref="KeyEventCodes"/>.</summary>
        static readonly ImmutableArray<(string Alias, KeyEventCodes Code)> s_aliases =
        [
            ..Enum
               .GetValues<KeyEventCodes>()
               .Select(x => (Alias: $"{x}", Code: x))
               .Where(x => x.Alias.StartsWith(KeyPrefix))
               .Select(x => x with { Alias = x.Alias[KeyPrefix.Length..] }),
        ];

        /// <summary>Contains the state of the keys.</summary>
        readonly bool[,] _keys = new bool[sizeof(Columns) * BitsInByte, KeyCount];

        /// <summary>Contains the number of times a key is pressed.</summary>
        readonly short[] _counters = new short[sizeof(Columns) * BitsInByte];

        /// <summary>Watches over <c>/dev/input</c> for new devices to listen to.</summary>
        readonly FileSystemWatcher? _watcher;

        /// <summary>Contains every input device.</summary>
        List<Device> _devices = [];

        /// <summary>Initializes a new instance of the <see cref="InputProvider.Evdev"/> class.</summary>
        /// <param name="warnings">The warnings.</param>
        public Evdev(out ImmutableArray<Exception> warnings)
        {
            void InputFileCreated(object caller, FileSystemEventArgs e)
            {
                if (Device.TryOpen(e.FullPath, out var dev, out _))
                    _devices.Add(dev);
            }

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
                warnings = accumulator.DrainToImmutable();
                return;
            }

            foreach (var device in paths)
                if (Device.TryOpen(device, out var dev, out e))
                    _devices.Add(dev);
                else
                    accumulator.Add(e);

            _watcher = new("/dev/input", "event*") { EnableRaisingEvents = true };
            _watcher.Created += InputFileCreated;
            warnings = accumulator.DrainToImmutable();
        }

        /// <inheritdoc />
        public override void Clear() => _keys.AsSpan2D().Clear();

        /// <inheritdoc />
        public override void Dispose()
        {
            foreach (var device in _devices)
                device.Dispose();

            _devices = [];
        }

        /// <inheritdoc />
        public override bool Add(Columns key, scoped ReadOnlySpan<char> value) =>
            TryParse(value) is { } code && (_keys.AsSpan2D()[key.ToIndex(), (int)code] = true);

        /// <inheritdoc />
        public override string GetValidValues() => Enum.GetValues<KeyEventCodes>().Conjoin();

        /// <inheritdoc />
        public override Columns Poll()
        {
            ReadOnlySpan2D<bool> keys = _keys;

            foreach (var device in _devices.AsSpan())
                while (device.Next() is var (state, code))
                    if (state is not null)
                        for (var i = 0; i < keys.Height; i++)
                            if (keys[i, code])
                                _ = state is Pressed ? _counters[i]++ : _counters[i]--;

            var column = Columns.None;

            for (var i = 0; i < _counters.Length; i++)
                if (_counters[i] > 0)
                    column |= i.ToColumns();

            return column;
        }

        /// <summary>Tries to parse the <see cref="KeyEventCodes"/> from <paramref name="value"/>.</summary>
        /// <param name="value">The <see cref="KeyEventCodes"/> to parse.</param>
        /// <returns>The parsed <see cref="KeyEventCodes"/> if successful; otherwise, <see langword="null"/>.</returns>
        static KeyEventCodes? TryParse(scoped ReadOnlySpan<char> value)
        {
            foreach (var (alias, code) in s_aliases)
                if (alias.EqualsIgnoreCase(value))
                    return code;

            return Enum.TryParse(value, true, out KeyEventCodes e) ? e : null;
        }
    }
}
