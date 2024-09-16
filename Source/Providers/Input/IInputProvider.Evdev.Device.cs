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

            /// <summary>Whether the next call to <see cref="Next"/> is synchronous.</summary>
            bool _syncNext;

            /// <summary>The file descriptor.</summary>
            int _fd;

            /// <summary>The device pointer.</summary>
            nint _device;

            /// <summary>Initializes a new instance of the <see cref="Device"/> class.</summary>
            /// <param name="fd">The file descriptor.</param>
            /// <param name="dev">The device pointer.</param>
            Device(int fd, nint dev) => (_fd, _device) = (fd, dev);

            /// <summary>Gets the name of the device.</summary>
            [SupportedOSPlatform("freebsd"), SupportedOSPlatform("linux")]
            string? Name => Marshal.PtrToStringUTF8(_device is not 0 ? DeviceName(_device) : 0);

            /// <inheritdoc />
            public override string ToString() =>
                (OperatingSystem.IsFreeBSD() || OperatingSystem.IsLinux() ? Name : null) ?? "<null device pointer>";

            /// <summary>Gets the next input.</summary>
            public InputEvent? Next =>
                OperatingSystem.IsFreeBSD() || OperatingSystem.IsLinux()
                    ? NextEvent(_device, _syncNext && !((_syncNext = false) is var _) ? 1u : 2, out var ev) switch
                    {
                        1 when (_syncNext = true) is var _ => ev,
                        >= 0 => ev,
                        _ => null,
                    }
                    : null;

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
                device = null;

                if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
                {
                    error = new PlatformNotSupportedException(
                        "'evdev' is not supported outside of Linux or FreeBSD. Please use 'sdl' instead."
                    );

                    return false;
                }

                if (inputPath is null)
                {
                    error = new ArgumentNullException(nameof(inputPath));
                    return false;
                }

                if (OpenFileDescriptor(inputPath, 0x800) is var file && file < 0)
                {
                    error = new Win32Exception();
                    return false;
                }

                var e = DeviceFromFileDescriptor(file, out var dev);

                if (e is not 0)
                {
                    // `libevdev_new_from_fd` automatically calls `libevdev_free` on failure, no need to call it here.
                    CloseFileDescriptor(file);
                    error = new IOException($"'libevdev_new_from_fd' failed with error code {e}.");
                    return false;
                }

                device = new(file, dev);
                error = null;
                return true;
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

                CloseFileDescriptor(_fd);
                _fd = 0;
            }

            [LibraryImport(C, EntryPoint = "close"), SupportedOSPlatform("freebsd"), SupportedOSPlatform("linux")]
            private static partial void CloseFileDescriptor(int fd);

            [LibraryImport(C, EntryPoint = "open", SetLastError = true, StringMarshalling = StringMarshalling.Utf8),
             SupportedOSPlatform("freebsd"),
             SupportedOSPlatform("linux")]
            private static partial int OpenFileDescriptor(string path, int flags);

            [LibraryImport(Lib, EntryPoint = "libevdev_free"),
             SupportedOSPlatform("freebsd"),
             SupportedOSPlatform("linux")]
            private static partial void FreeDevice(nint device);

            [LibraryImport(Lib, EntryPoint = "libevdev_new_from_fd"),
             SupportedOSPlatform("freebsd"),
             SupportedOSPlatform("linux")]
            private static partial int DeviceFromFileDescriptor(int fd, out nint device);

            [LibraryImport(Lib, EntryPoint = "libevdev_next_event"),
             SupportedOSPlatform("freebsd"),
             SupportedOSPlatform("linux")]
            private static partial int NextEvent(nint dev, uint flags, out InputEvent ev);

            [LibraryImport(Lib, EntryPoint = "libevdev_get_name"),
             SupportedOSPlatform("freebsd"),
             SupportedOSPlatform("linux")]
            private static partial nint DeviceName(nint device);
        }
    }
}
