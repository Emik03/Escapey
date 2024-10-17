// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Audio;

partial interface IAudioProvider
{
    sealed partial class PipeWire
    {
        /// <summary>Represents the <c>spa_buffer</c> type.</summary>
        [StructLayout(LayoutKind.Sequential)]
        readonly unsafe struct SpaBuffer
        {
            /// <summary>Padding.</summary>
            readonly uint _nMetas;

            /// <summary>Contains the number of elements in the array.</summary>
            internal readonly int _nDatas;

            /// <summary>Padding.</summary>
            readonly nint _metas;

            /// <summary>Contains the pointer to the array.</summary>
            internal readonly SpaData* _datas;
        }
    }
}
