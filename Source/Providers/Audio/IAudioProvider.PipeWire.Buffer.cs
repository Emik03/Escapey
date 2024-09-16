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
            internal readonly unsafe SpaBuffer* _buffer;

            /// <summary>Padding.</summary>
            readonly nint _userData;

            /// <summary>Padding.</summary>
            readonly ulong _size, _requested;

            /// <summary>Gets a value determining whether there is data available to read.</summary>
            public unsafe bool HasData => _buffer->_datas->_data is not null;

            /// <inheritdoc cref="Span{T}.CopyTo"/>
            public unsafe void CopyTo(Span<float> destination) => _buffer->_datas->AsSpan.CopyTo(destination);

            /// <inheritdoc />
            public override unsafe string ToString() => _buffer->_datas->_data is null ? "[]" : _buffer->ToString();
        }
    }
}
