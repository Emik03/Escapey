// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Audio;

partial interface IAudioProvider
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
            internal readonly float* _data;

            /// <summary>Contains the pointer to the <c>spa_chunk</c> type.</summary>
            internal readonly SpaChunk* _chunk;

            /// <summary>Gets the <see cref="float"/> array.</summary>
            /// <returns>The <see cref="float"/> array.</returns>
            public ReadOnlySpan<float> AsSpan => new(_data, _chunk->_size / sizeof(float));

            /// <inheritdoc />
            public override string ToString() => $"[{AsSpan.ToArray().Conjoin()}]";
        }
    }
}
