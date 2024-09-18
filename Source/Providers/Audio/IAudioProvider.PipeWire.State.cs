// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Audio;

partial interface IAudioProvider
{
    sealed partial class PipeWire
    {
        /// <summary>Contains the data passed around within PipeWire callbacks.</summary>
        [StructLayout(LayoutKind.Auto)]
        partial struct State // ReSharper disable UnassignedField.Local
        {
            /// <summary>The amount of latency.</summary>
            static readonly string s_latency = $"{Length}/48000";

            /// <summary>The temporary buffers.</summary>
            unsafe fixed float _buffer[4800];

            /// <summary>The pointers to PipeWire objects.</summary>
            nint _loop, _stream;

            /// <summary>The index of the next buffer.</summary>
            int _index;

            /// <summary>The flag for buffers.</summary>
            bool _hasNewData;

            /// <summary>Executes the entry point for PipeWire.</summary>
            /// <param name="data">The pointer to this function call's stack-allocated memory.</param>
            /// <exception cref="Win32Exception">An error occured in the PipeWire library.</exception>
            public static unsafe void Run(out State* data)
            {
                State state = new();
                data = &state;

                if (PropertiesNew(0, 0) is var props &&
                    PropertiesSet(props, "application.name", nameof(Escapey)) < 0 ||
                    PropertiesSet(props, "node.name", nameof(Escapey)) < 0 || // NOTE: Must be unique for all instances.
                    PropertiesSet(props, "config.name", "client-rt.conf") < 0 ||
                    PropertiesSet(props, "media.type", "Audio") < 0 ||
                    PropertiesSet(props, "media.category", "Capture") < 0 ||
                    PropertiesSet(props, "media.role", "Music") < 0 ||
                    PropertiesSet(props, "node.latency", s_latency) < 0)
                {
                    Win32Exception ex = new();
                    PropertiesFree(props);
                    throw ex;
                }

                state._loop = ThreadLoopNew(nameof(Escapey));
                ThreadLoopLock(state._loop);

                if (ThreadLoopStart(state._loop) is not 0)
                {
                    Win32Exception ex = new();
                    ThreadLoopUnlock(state._loop);
                    ThreadLoopDestroy(state._loop);
                    throw ex;
                }

                state._stream =
                    StreamNewSimple(ThreadLoopGetLoop(state._loop), nameof(Escapey), props, new(&OnProcess), &state);

                try
                {
                    fixed (byte* param = StreamParameterBlob)
                        if (StreamConnect(state._stream, 0, -1, 21, param) is not 0)
                            throw new Win32Exception();

                    ThreadLoopWait(state._loop);
                }
                finally
                {
                    ThreadLoopUnlock(state._loop);
                    ThreadLoopStop(state._loop);
                    StreamDestroy(state._stream);
                    ThreadLoopDestroy(state._loop);
                }
            }

            /// <summary>Gets the current buffer.</summary>
            /// <param name="data">The state to get the buffer from.</param>
            /// <returns>The current buffer.</returns>
            public static unsafe ReadOnlySpan<float> Current(State* data) =>
                data is not null && data->_hasNewData && !(data->_hasNewData = false)
                    ? new Span<float>(data->_buffer + data->_index * Length, Length)
                    : default;

            /// <summary>Interrupts the thread loop.</summary>
            public readonly void Stop()
            {
                if (_loop is not 0)
                    ThreadLoopSignal(_loop);
            }

            /// <summary>Copies the next PipeWire buffer into its own temporary buffers.</summary>
            /// <param name="data">The state containing the temporary buffer.</param>
            static unsafe void OnProcess(State* data)
            {
                if (DequeueBuffer(data->_stream) is var buffer && buffer->AsSpan is not [] and var span)
                {
                    var i = (data->_index + 1).Mod(4800 / Length);
                    span.CopyTo(new(data->_buffer + i * Length, Length));
                    data->_index = i;
                    data->_hasNewData = true;
                }

                QueueBuffer(data->_stream, buffer);
            }

            [LibraryImport(Lib, EntryPoint = "pw_properties_free")]
            private static partial void PropertiesFree(nint properties);

            [LibraryImport(Lib, EntryPoint = "pw_stream_queue_buffer")]
            private static unsafe partial void QueueBuffer(nint stream, Buffer* buffer);

            [LibraryImport(Lib, EntryPoint = "pw_stream_destroy")]
            private static partial void StreamDestroy(nint stream);

            [LibraryImport(Lib, EntryPoint = "pw_thread_loop_destroy")]
            private static partial void ThreadLoopDestroy(nint loop);

            [LibraryImport(Lib, EntryPoint = "pw_thread_loop_lock")]
            private static partial void ThreadLoopLock(nint loop);

            [LibraryImport(Lib, EntryPoint = "pw_thread_loop_signal")]
            private static unsafe partial void
                ThreadLoopSignal(nint loop, [MarshalAs(UnmanagedType.Bool)] bool waitForAccept = false);

            [LibraryImport(Lib, EntryPoint = "pw_thread_loop_stop")]
            private static partial void ThreadLoopStop(nint loop);

            [LibraryImport(Lib, EntryPoint = "pw_thread_loop_unlock")]
            private static partial void ThreadLoopUnlock(nint loop);

            [LibraryImport(Lib, EntryPoint = "pw_thread_loop_wait")]
            private static partial void ThreadLoopWait(nint loop);

            [LibraryImport(Lib, EntryPoint = "pw_properties_set", StringMarshalling = StringMarshalling.Utf8)]
            private static partial int PropertiesSet(nint properties, string key, string value);

            [LibraryImport(Lib, EntryPoint = "pw_stream_connect")]
            private static unsafe partial int
                StreamConnect(nint stream, int direction, int targetId, int flags, in byte* param, int nParams = 1);

            [LibraryImport(Lib, EntryPoint = "pw_thread_loop_start")]
            private static partial int ThreadLoopStart(nint loop);

            [LibraryImport(Lib, EntryPoint = "pw_properties_new")]
            private static partial nint PropertiesNew(nint key, nint args);

            [LibraryImport(Lib, EntryPoint = "pw_stream_new_simple", StringMarshalling = StringMarshalling.Utf8)]
            private static unsafe partial nint
                StreamNewSimple(nint loop, string name, nint props, in StreamEvents events, State* data);

            [LibraryImport(Lib, EntryPoint = "pw_thread_loop_get_loop")]
            private static partial nint ThreadLoopGetLoop(nint loop);

            [LibraryImport(Lib, EntryPoint = "pw_thread_loop_new", StringMarshalling = StringMarshalling.Utf8)]
            private static partial nint ThreadLoopNew(string? name, nint props = 0);

            [LibraryImport(Lib, EntryPoint = "pw_stream_dequeue_buffer")]
            private static unsafe partial Buffer* DequeueBuffer(nint stream);
        }
    }
}
