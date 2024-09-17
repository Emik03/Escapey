// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Audio;

partial interface IAudioProvider
{
    /// <summary>Provides audio implementation with ALC.</summary>
    private sealed class Alc : IAudioProvider
    {
        /// <summary>The number of non-disposed instances of <see cref="IAudioProvider.Alc"/>.</summary>
        static int s_references;

        /// <summary>Contains the raw PCM data.</summary>
        readonly byte[] _pcm = new byte[8820];

        /// <summary>Determines whether this instance is disposed.</summary>
        bool _disposed;

        /// <summary>The current index to start writing from.</summary>
        int _i;

        /// <summary>Initializes a new instance of the <see cref="IAudioProvider.Alc"/> class.</summary>
        public Alc()
        {
            Microphone.Default.BufferDuration = TimeSpan.FromMilliseconds(100);
            Microphone.Default.BufferReady += Noop;
            Microphone.Default.Start();
            s_references++;
        }

        /// <inheritdoc />
        public float[] Imaginary { get; } = new float[Length];

        /// <inheritdoc />
        public float[] Real { get; } = new float[Length];

        /// <inheritdoc />
        public AudioSegment Segment { get; } = new();

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed && (_disposed = true) && --s_references is 0)
                Microphone.Default.Stop();
        }

        /// <inheritdoc />
        [MustUseReturnValue]
        public AudioSegment? Poll()
        {
            if (PollRaw().IsEmpty)
                return null;

            FFT(this);
            return Segment;
        }

        /// <inheritdoc />
        [MustUseReturnValue]
        public ReadOnlySpan<float> PollRaw()
        {
            if ((_i += Microphone.Default.GetData(_pcm, _i, _pcm.Length - _i)) < Length * sizeof(short))
                return [];

            ref var end = ref Unsafe.As<byte, short>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_pcm), _i));
            ref var real = ref MemoryMarshal.GetArrayDataReference(Real);
            ref var pcm = ref Unsafe.Add(ref end, -Length);
            _i = 0;

            while (Unsafe.IsAddressLessThan(pcm, end))
            {
                real = pcm / (float)short.MaxValue;
                real = ref Unsafe.Add(ref real, 1);
                pcm = ref Unsafe.Add(ref pcm, 1);
            }

            return Real;
        }
    }
}
