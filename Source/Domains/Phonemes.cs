// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Domains;

/// <summary>Represents every phoneme for predictions.</summary>
enum Phonemes
{
    [Phoneme("nothing")]
    Nothing,

    [Phoneme("typing")]
    Typing,

    [Phoneme("mashing")]
    Mashing,

    [Phoneme("breathing")]
    Breathing,

    [Phoneme("finger tapping")]
    FingerTapping,

    [Phoneme("foot tapping")]
    FootTapping,

    [Phoneme("chair rocking")]
    ChairRocking,

    [Phoneme("/æ/", Sprite.Mouth.Ah)]
    Ae,

    [Phoneme("/a/", Sprite.Mouth.Ah)]
    A,

    [Phoneme("/ä/", Sprite.Mouth.Ah)]
    Ah,

    [Phoneme("/ɑ/", Sprite.Mouth.Ah)]
    Ao,

    [Phoneme("/ʒ/", Sprite.Mouth.Dz)]
    Dj,

    [Phoneme("/ʃ/", Sprite.Mouth.Dz)]
    Sh,

    [Phoneme("/e/", Sprite.Mouth.E)]
    E,

    [Phoneme("/i/", Sprite.Mouth.E)]
    I,

    [Phoneme("/y/", Sprite.Mouth.E)]
    Y,

    [Phoneme("/f/", Sprite.Mouth.F)]
    F,

    [Phoneme("/v/", Sprite.Mouth.F)]
    V,

    [Phoneme("/m/", Sprite.Mouth.M)]
    M,

    [Phoneme("/n/", Sprite.Mouth.M)]
    N,

    [Phoneme("/s/", Sprite.Mouth.Nsl)]
    S,

    [Phoneme("/z/", Sprite.Mouth.Nsl)]
    Z,

    [Phoneme("/o/", Sprite.Mouth.O)]
    O,

    [Phoneme("/u/", Sprite.Mouth.O)]
    U,

    [Phoneme("/w/", Sprite.Mouth.O)]
    W,
}

/// <summary>Exposes the data from <see cref="PhonemeAttribute"/> within instances of <see cref="Phonemes"/>.</summary>
static class PhonemesExtensions
{
    /// <summary>Contains the cached value of every <see cref="PhonemeAttribute"/>.</summary>
    public static ImmutableArray<KeyValuePair<Phonemes, PhonemeAttribute>> Attributes { get; } =
        ImmutableCollectionsMarshal
           .AsImmutableArray(Enum.GetValues<Phonemes>()) // ReSharper disable once NullableWarningSuppressionIsUsed
           .ConvertAll(x => new KeyValuePair<Phonemes, PhonemeAttribute>(x, x.GetCustomAttribute<PhonemeAttribute>()!));

    /// <summary>Gets the display name.</summary>
    /// <param name="phoneme">The phoneme to get the display name of.</param>
    /// <returns>The display name of the parameter <paramref name="phoneme"/>.</returns>
    public static string DisplayName(this Phonemes phoneme) => Attributes[(int)phoneme].Value.ToString();

    /// <summary>Gets the corresponding mouth shape.</summary>
    /// <param name="phoneme">The phoneme to get the mouth shape of.</param>
    /// <returns>The mouth shape of the parameter <paramref name="phoneme"/>.</returns>
    public static Sprite.Mouth ToMouth(this Phonemes phoneme) => Attributes[(int)phoneme].Value.Mouth;
}
