// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Input;

partial interface IInputProvider
{
    sealed partial class Evdev
    {
        /// <summary>Represents an input device.</summary>
        sealed partial class Device : IDisposable
        {
            /// <summary>The name of the C library.</summary>
            const string C = "c";

            /// <summary>The library name to link against.</summary>
            const string Lib = "libevdev.so.2";

            /// <summary>Whether the next call to <see cref="Next"/> is asynchronous.</summary>
            bool _async = true;

            /// <summary>The file descriptor.</summary>
            int _fd;

            /// <summary>The device pointer.</summary>
            nint _device;

            /// <summary>Initializes a new instance of the <see cref="Device"/> class.</summary>
            /// <param name="fd">The file descriptor.</param>
            /// <param name="device">The device pointer.</param>
            Device(int fd, nint device) => (_fd, _device) = (fd, device);

            /// <summary>Gets a value indicating whether the device is initialized.</summary>
            [SupportedOSPlatformGuard("freebsd"), SupportedOSPlatformGuard("linux")]
            bool IsInitialized => _device is not 0;

            /// <summary>Gets the name of the device.</summary>
            string? Name => Marshal.PtrToStringUTF8(IsInitialized ? DeviceName(_device) : 0);

            /// <summary>Attempts to create a new <see cref="Evdev"/> from <paramref name="device"/>.</summary>
            /// <param name="inputPath">The path to the input device, typically within <c>/dev/input/</c>.</param>
            /// <param name="device">The inputPath to the input device.</param>
            /// <param name="error">The error.</param>
            /// <returns>
            /// The result of attempting to create a new <see cref="Evdev"/> from <paramref name="device"/>.
            /// </returns>
            public static bool TryOpen(
                string? inputPath,
                [NotNullWhen(true)] out Device? device,
                [NotNullWhen(false)] out Exception? error
            )
            {
                if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
                {
                    const string E = "'evdev' is not supported outside of Linux or FreeBSD. Please use 'sdl' instead.";
                    (device, error) = (null, new PlatformNotSupportedException(E));
                    return false;
                }

                if (inputPath is null)
                    return !(((device, error) = (null, new ArgumentNullException(nameof(inputPath)))) is var _);

                if (OpenFile(inputPath, 0x800) is var file && file < 0)
                    return !(((device, error) = (null, new Win32Exception())) is var _);

                if (FromFile(file, out var d) is not (var e and not 0))
                    return ((device, error) = (new(file, d), null)) is var _;

                // `libevdev_new_from_fd` automatically calls `libevdev_free` on failure, no need to call it here.
                CloseFile(file);
                (device, error) = (null, new IOException($"'libevdev_new_from_fd' failed with error code {e}."));
                return false;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
                    return;

                if (_device is not 0)
                {
                    FreeDevice(_device);
                    _device = 0;
                }

                if (_fd is 0)
                    return;

                CloseFile(_fd);
                _fd = 0;
            }

            /// <inheritdoc />
            public override string ToString() => Name ?? "<null device pointer>";

            /// <summary>Gets the next input.</summary>
            public InputEvent? Next() =>
                IsInitialized
                    ? NextEvent(_device, _async.ToByte() + 1, out var ev) switch
                    {
                        1 when (_async = false) is var _ => ev,
                        >= 0 when (_async = true) is var _ => ev,
                        _ => null,
                    }
                    : null;

            [LibraryImport(C, EntryPoint = "close"), SupportedOSPlatform("freebsd"), SupportedOSPlatform("linux")]
            private static partial void CloseFile(int fd);

            [LibraryImport(C, EntryPoint = "open", SetLastError = true, StringMarshalling = StringMarshalling.Utf8),
             SupportedOSPlatform("freebsd"),
             SupportedOSPlatform("linux")]
            private static partial int OpenFile(string path, int flags);

            [LibraryImport(Lib, EntryPoint = "libevdev_free"),
             SupportedOSPlatform("freebsd"),
             SupportedOSPlatform("linux")]
            private static partial void FreeDevice(nint device);

            [LibraryImport(Lib, EntryPoint = "libevdev_new_from_fd"),
             SupportedOSPlatform("freebsd"),
             SupportedOSPlatform("linux")]
            private static partial int FromFile(int fd, out nint device);

            [LibraryImport(Lib, EntryPoint = "libevdev_next_event"),
             SupportedOSPlatform("freebsd"),
             SupportedOSPlatform("linux")]
            private static partial int NextEvent(nint dev, int flags, out InputEvent ev);

            [LibraryImport(Lib, EntryPoint = "libevdev_get_name"),
             SupportedOSPlatform("freebsd"),
             SupportedOSPlatform("linux")]
            private static partial nint DeviceName(nint device);
        }
    }
}
