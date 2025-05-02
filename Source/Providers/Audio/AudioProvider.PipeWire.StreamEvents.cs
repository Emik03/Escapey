// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Audio;

partial class AudioProvider
{
    sealed partial class PipeWire
    {
        /// <summary>Represents the <c>pw_stream_events</c> type.</summary>
        [StructLayout(LayoutKind.Sequential)]
        readonly unsafe partial struct StreamEvents(delegate*<State*, void> process, int version = 2)
        {
            /// <summary>The version, which has to be this number.</summary>
            readonly int _version = version;

            /// <summary>Padding.</summary>
            readonly nint _destroy, _stateChanged, _controlInfo, _ioChanged, _paramChanged, _addBuffer, _removeBuffer;

            /// <summary>Invoked when a buffer is ready.</summary>
            readonly delegate*<State*, void> _process = process;

            /// <summary>Padding.</summary>
            readonly nint _drained, _command, _triggerDone;
        }
    }
}
