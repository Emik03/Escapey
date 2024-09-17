// SPDX-License-Identifier: MPL-2.0
#if !ANDROID
using var pipeWire = IAudioProvider.FromAlias("pipewire", out var exceptions);
exceptions.ThrowAny();

while (true)
{
    while (!pipeWire.Poll()) { }

    Console.Clear();
    DrawSlider(pipeWire.Segment.NormalizationFactor);
    await Task.Delay(4);
}

static void DrawSlider(float f)
{
    const int Girth = 60;

    Console.Write('[');
    var b = (int)(f * Girth);

    for (var i = 0; i < b; i++)
        Console.Write('*');

    for (var i = b; i < Girth; i++)
        Console.Write(' ');

    Console.Write(']');
    Console.WriteLine();
}
#endif
