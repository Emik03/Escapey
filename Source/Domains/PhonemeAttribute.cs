// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Domains;

/// <summary>Represents a phoneme, used within <see cref="Phonemes"/>.</summary>
/// <param name="display">The text to display.</param>
/// <param name="mouth">The mouth that represents this phoneme.</param>
[AttributeUsage(AttributeTargets.Field)]
sealed class PhonemeAttribute(string display, Sprite.Mouth mouth = Sprite.Mouth.Angry) : Attribute
{
    /// <summary>The mouth that this phoneme represents.</summary>
    public Sprite.Mouth Mouth => mouth;

    /// <inheritdoc />
    public override string ToString() => display;
}
