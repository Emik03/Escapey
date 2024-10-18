// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Domains;

using static Sprite.Mouth;

/// <summary>Represents the set of buttons being held.</summary>
[Flags]
enum Columns : ushort
{
    /// <summary>Represents no buttons being held.</summary>
    None,

    /// <summary>Represents the first button being held.</summary>
    First,

    /// <summary>Represents the second button being held.</summary>
    Second,

    /// <summary>Represents the third button being held.</summary>
    Third = 1 << 2,

    /// <summary>Represents the fourth button being held.</summary>
    Fourth = 1 << 3,

    /// <summary>Represents all buttons being held.</summary>
    All = First | Second | Third | Fourth,

    /// <summary>Used to hide the keys.</summary>
    Hide = 1 << 4,

    /// <summary>Used to make the eyes and mouth colorful.</summary>
    Rainbow = 1 << 5,

    /// <summary>Used to indicate the expression be angry.</summary>
    Angry = 1 << 6,

    /// <summary>Used to indicate the expression be bored.</summary>
    Bored = 1 << 7,

    /// <summary>Used to indicate the expression be concentrated.</summary>
    Concentrated = 1 << 8,

    /// <summary>Used to indicate the expression be confused.</summary>
    Confused = 1 << 9,

    /// <summary>Used to indicate the expression be frowning.</summary>
    Frown = 1 << 10,

    /// <summary>Used to indicate the expression be happy.</summary>
    Happy = 1 << 11,

    /// <summary>Used to indicate the expression be happy, with a raised eyebrow.</summary>
    HappyEyebrow = 1 << 12,

    /// <summary>Used to indicate the expression be laughter.</summary>
    Laughter = 1 << 13,

    /// <summary>Used to indicate the expression be scared.</summary>
    Scared = 1 << 14,

    /// <summary>Used to indicate the expression be upset.</summary>
    Upset = 1 << 15,
}

/// <summary>Extensions for <see cref="Columns"/>.</summary>
#pragma warning disable MA0048
static class ColumnExtensions
#pragma warning restore MA0048
{
    /// <summary>Determines if the mouth is speaking.</summary>
    /// <param name="mouth">The mouth to test.</param>
    /// <returns>
    /// The value <see langword="true"/> if the mouth is speaking; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSpeaking(this Sprite.Mouth mouth) => mouth is Ah or Dz or E or F or M or Nsl or O;

    /// <summary>Converts the column to the index.</summary>
    /// <param name="column">The column.</param>
    /// <returns>The index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ToIndex(this Columns column) => (byte)BitOperations.TrailingZeroCount((int)column);

    /// <summary>Inverts the columns.</summary>
    /// <param name="column">The columns to invert.</param>
    /// <returns>The inverted columns.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Columns Invert(this Columns column) =>
        column & ~Columns.All ^
        (Columns)(((ushort)column & 1) << 3) ^
        (Columns)(((ushort)column & 2) << 1) ^
        (Columns)(((ushort)column & 4) >> 1) ^
        (Columns)(((ushort)column & 8) >> 3);

    /// <summary>Inverts the columns.</summary>
    /// <param name="column">The columns to invert.</param>
    /// <param name="condition">The condition on whether to invert.</param>
    /// <returns>The inverted columns.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Columns InvertIf(this Columns column, bool condition) => condition ? column.Invert() : column;

    /// <summary>Converts the index to the column.</summary>
    /// <param name="index">The index.</param>
    /// <returns>The column.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Columns ToColumns(this int index) => (Columns)(1 << index);

    /// <summary>Converts the <see cref="Sprite.Mouth"/> to the IPA representations.</summary>
    /// <param name="mouth">The <see cref="Sprite.Mouth"/> to convert.</param>
    /// <returns>The IPA representations of the parameter <paramref name="mouth"/>.</returns>
    public static ImmutableArray<string> ToIPAs(this Sprite.Mouth mouth) =>
        mouth switch
        {
            Ah => ["/æ/", "/a/", "/ä/"],
            Dz => ["/z/", "/ʒ/", "/ʃ/"],
            E => ["/e/", "/i/", "/y/"],
            F => ["/f/", "/v/"],
            M => ["/m/"],
            Nsl => ["/s/", "/l/"],
            O => ["/o/", "/ʊ/", "/u/", "/w/"],
            _ => ["nothing", "keyboard typing", "keyboard mashing", "loud breathing", "foot tapping", "chair rocking"],
        };

    /// <summary>Converts the <see cref="Columns"/> to the <see cref="Sprite.Arm.Left"/>.</summary>
    /// <param name="column">The <see cref="Columns"/> to convert.</param>
    /// <returns>The <see cref="Sprite.Arm.Left"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sprite.Arm.Left ToLeftArm(this Columns column) =>
        (Sprite.Arm.Left)(column.Has(Columns.First).ToByte() + column.Has(Columns.Second).ToByte() * 2);

    /// <summary>Converts the <see cref="Columns"/> to the <see cref="Sprite.Arm.Right"/>.</summary>
    /// <param name="column">The <see cref="Columns"/> to convert.</param>
    /// <returns>The <see cref="Sprite.Arm.Right"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sprite.Arm.Right ToRightArm(this Columns column) =>
        (Sprite.Arm.Right)(column.Has(Columns.Third).ToByte() + column.Has(Columns.Fourth).ToByte() * 2);

    /// <summary>Converts the <see cref="Columns"/> to the <see cref="Sprite.Eyes"/></summary>
    /// <param name="column">The <see cref="Columns"/> to convert.</param>
    /// <returns>The <see cref="Sprite.Eyes"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sprite.Eyes ToEyes(this Columns column) =>
        (Sprite.Eyes)BitOperations.TrailingZeroCount((ushort)column >> Columns.Angry.ToIndex());

    /// <summary>Converts the <see cref="Columns"/> to the <see cref="Sprite.Mouth"/></summary>
    /// <param name="column">The <see cref="Columns"/> to convert.</param>
    /// <param name="fallback">The mouth to use as fallback if no mouth is set.</param>
    /// <returns>
    /// The <see cref="Sprite.Mouth"/> of the <see cref="Columns"/>, or <paramref name="fallback"/> if no mouth is set.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sprite.Mouth ToMouth(this Columns column, ref Sprite.Mouth fallback) =>
        (ushort)column >> Columns.Angry.ToIndex() is not 0 and var shift
            ? fallback = (Sprite.Mouth)BitOperations.TrailingZeroCount(shift)
            : fallback;
}
