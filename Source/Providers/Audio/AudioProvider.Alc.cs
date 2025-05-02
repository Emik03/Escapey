// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Audio;

partial class AudioProvider
{
    /// <summary>Provides audio implementation with ALC.</summary>
    sealed class Alc : AudioProvider
    {
        /// <summary>Gets the current microphone.</summary>
        static readonly Microphone s_microphone =
            Microphone.All.Where(x => x.Name is not null && x.Name == PreferredMicrophone).FirstOr(Microphone.Default);

        /// <summary>Contains the raw PCM data.</summary>
        readonly byte[] _pcm = new byte[8820];

        /// <summary>Contains the PCM data converted to <see cref="float"/>.</summary>
        readonly float[] _real = new float[Length];

        /// <summary>The current index to start writing from.</summary>
        int _i;

        /// <summary>Initializes a new instance of the <see cref="AudioProvider.Alc"/> class.</summary>
        public Alc()
        {
            s_microphone.BufferDuration = TimeSpan.FromMilliseconds(100);
            s_microphone.BufferReady += Noop;
            s_microphone.Start();
        }

        /// <inheritdoc />
        public override AudioSegment Segment { get; } = new();

        /// <inheritdoc />
        public override void Dispose() { }

        /// <inheritdoc />
        [MustUseReturnValue]
        public override AudioSegment? Poll()
        {
            if (PollRaw().IsEmpty)
                return null;

            Segment.Forward(_real);
            return Segment;
        }

        /// <inheritdoc />
        [MustUseReturnValue]
        public override Span<float> PollRaw()
        {
            if ((_i += s_microphone.GetData(_pcm, _i, _pcm.Length - _i)) < Length * sizeof(short))
                return [];

            ref var end = ref Unsafe.As<byte, short>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_pcm), _i));
            ref var real = ref MemoryMarshal.GetArrayDataReference(_real);
            ref var pcm = ref Unsafe.Subtract(ref end, Length);
            _i = 0;

            while (Unsafe.IsAddressLessThan(pcm, end))
            {
                real = pcm / (float)short.MaxValue;
                real = ref Unsafe.Add(ref real, 1);
                pcm = ref Unsafe.Add(ref pcm, 1);
            }

            return _real;
        }
    }
}
