// SPDX-License-Identifier: LicenseRef-PolyForm-Strict
namespace Escapey.Providers.Input;

abstract partial class InputProvider
{
    sealed partial class Evdev
    {
        sealed partial class Device
        {
            /// <summary>Represents the <c>input_event</c> type.</summary>
            /// <param name="Time">The time of the event.</param>
            /// <param name="Type">The type of the event.</param>
            /// <param name="Code">The code of the event.</param>
            /// <param name="Value">The value of the event.</param>
            [StructLayout(LayoutKind.Sequential)]
            public readonly partial record struct InputEvent(
                [UsedImplicitly] TimeValue Time,
                ushort Type,
                ushort Code,
                int Value
            )
            {
                /// <summary>
                /// When <see cref="Type"/> matches this constant, the <see cref="Value"/> is a button state.
                /// </summary>
                const int EvKeyType = 1;

                /// <summary>Gets the value indicating the button state.</summary>
                public ButtonState? KeyState =>
                    Value switch
                    {
                        0 when Type is EvKeyType => ButtonState.Released,
                        1 when Type is EvKeyType => ButtonState.Pressed,
                        _ => null,
                    };

                /// <summary>Deconstructs the <see cref="InputEvent"/>.</summary>
                /// <param name="keyState">The key state.</param>
                /// <param name="code">The code.</param>
                public void Deconstruct(out ButtonState? keyState, out ushort code) =>
                    (keyState, code) = (KeyState, Code);

                /// <inheritdoc />
                public override string ToString() =>
                    OperatingSystem.IsFreeBSD() || OperatingSystem.IsLinux()
                        ? $"{Marshal.PtrToStringUTF8(TypeName(Type))}: {Marshal.PtrToStringUTF8(CodeName(Type, Code))}"
                        : "";

                [LibraryImport(Lib, EntryPoint = "libevdev_event_code_get_name"),
                 SupportedOSPlatform("freebsd"),
                 SupportedOSPlatform("linux")]
                private static partial nint CodeName(uint type, uint code);

                [LibraryImport(Lib, EntryPoint = "libevdev_event_type_get_name"),
                 SupportedOSPlatform("freebsd"),
                 SupportedOSPlatform("linux")]
                private static partial nint TypeName(uint type);
            }
        }
    }
}
