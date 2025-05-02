// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Input;

abstract partial class InputProvider
{
    sealed partial class Evdev
    {
        sealed partial class Device
        {
            /// <summary>Represents the <c>timeval</c> type.</summary>
            /// <param name="Seconds">The amount of seconds since <see cref="DateTime.UnixEpoch"/>.</param>
            /// <param name="Microseconds">The microseconds since <see cref="Seconds"/>.</param>
            [StructLayout(LayoutKind.Sequential)]
            public readonly record struct TimeValue(long Seconds, long Microseconds)
            {
                /// <summary>Implicitly converts the <see cref="TimeValue"/> to the <see cref="DateTime"/>.</summary>
                /// <param name="time">The <see cref="TimeValue"/> to convert.</param>
                /// <returns>The converted <see cref="DateTime"/>.</returns>
                public static implicit operator DateTime(TimeValue time) =>
                    DateTime.UnixEpoch.AddSeconds(time.Seconds).AddMicroseconds(time.Microseconds);

                /// <inheritdoc />
                public override string ToString() => ((DateTime)this).ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
