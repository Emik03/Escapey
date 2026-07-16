// SPDX-License-Identifier: LicenseRef-PolyForm-Strict
namespace Escapey.Providers.Audio;

partial class AudioProvider
{
    sealed partial class PipeWire
    {
        /// <summary>Represents the <c>spa_data</c> type.</summary>
        [StructLayout(LayoutKind.Sequential)]
        readonly unsafe struct SpaData
        {
            /// <summary>Padding.</summary>
            readonly uint _type, _flags;

            /// <summary>Padding.</summary>
            readonly long _fd;

            /// <summary>Padding.</summary>
            readonly uint _mapOffset, _maxSize;

            /// <summary>Contains the pointer to the <see cref="float"/> array.</summary>
            readonly float* _data;

            /// <summary>Contains the pointer to the <c>spa_chunk</c> type.</summary>
            readonly SpaChunk* _chunk;

            /// <summary>Gets the <see cref="float"/> array.</summary>
            /// <returns>The <see cref="float"/> array.</returns>
            public ReadOnlySpan<float> AsSpan => _data is null ? default : new(_data, _chunk->_size / sizeof(float));
        }
    }
}
