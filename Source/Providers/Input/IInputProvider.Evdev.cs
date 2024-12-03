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

        /// <summary>Contains the number of times a key is pressed.</summary>
        readonly int[] _counts = new int[sizeof(Columns) * BitsInByte];

        /// <summary>Watches over <c>/dev/input</c> for new devices to listen to.</summary>
        readonly FileSystemWatcher? _watcher;

        /// <summary>Contains every input device.</summary>
        List<Device> _devices = [];

        /// <summary>Initializes a new instance of the <see cref="IInputProvider.Evdev"/> class.</summary>
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
        public void Dispose()
        {
            foreach (var device in _devices)
                device.Dispose();

            _devices = [];
        }

        /// <inheritdoc />
        public bool Add(Columns key, scoped ReadOnlySpan<char> value)
        {
            if (TryParse(value) is not { } code)
                return false;

            Span2D<bool> k = _keys;
            k[key.ToIndex(), (int)code] = true;
            return true;
        }

        /// <inheritdoc />
        public override string ToString() => $"[{_devices.Conjoin()}]";

        /// <inheritdoc />
        // ReSharper disable once CognitiveComplexity
        public Columns Poll()
        {
            ReadOnlySpan2D<bool> keys = _keys;

            foreach (var device in _devices.AsSpan())
                while (device.Next() is var (state, code))
                    if (state is not null && (_keyState[code] = state is Pressed) is var _)
                        for (var i = 0;
                            i < keys.Height && (!keys[i, code] || (_counts[i] += state is Pressed ? 1 : -1) is var _);
                            i++) { }

            var col = Columns.None;

            for (var i = 0; i < _counts.Length; i++)
                if (_counts[i] > 0)
                    col |= i.ToColumns();

            return col;
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
