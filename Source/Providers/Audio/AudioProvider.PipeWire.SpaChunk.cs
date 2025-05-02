// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Audio;

partial class AudioProvider
{
    sealed partial class PipeWire
    {
        /// <summary>Represents the <c>spa_chunk</c> type.</summary>
        [StructLayout(LayoutKind.Sequential, Size = 16)]
        readonly struct SpaChunk
        {
            /// <summary>Padding.</summary>
            readonly uint _offset;

            /// <summary>Contains the size of the <see cref="float"/> array.</summary>
            internal readonly int _size;
        }
    }
}
