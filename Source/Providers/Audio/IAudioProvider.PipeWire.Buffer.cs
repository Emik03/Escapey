// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Audio;

partial interface IAudioProvider
{
    sealed partial class PipeWire
    {
        /// <summary>Represents the <c>pw_buffer</c> type.</summary>
        [StructLayout(LayoutKind.Sequential)]
        readonly partial struct Buffer
        {
            /// <summary>Contains the pointer to the <see cref="SpaBuffer"/>.</summary>
            readonly unsafe SpaBuffer* _buffer;

            /// <summary>Padding.</summary>
            readonly nint _userData;

            /// <summary>Padding.</summary>
            readonly ulong _size, _requested;

            /// <summary>Gets the <see cref="float"/> array.</summary>
            /// <returns>The <see cref="float"/> array.</returns>
            public unsafe ReadOnlySpan<float> AsSpan => _buffer->_datas->AsSpan;
        }
    }
}
