// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Sprites;

/// <summary>Associates the applied member with a <see cref="Sprite"/> or directory of sprites.</summary>
/// <param name="path"></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Field)]
sealed class SpriteAttribute([UriString, StringSyntax(StringSyntaxAttribute.Uri)] string? path = null) : Attribute
{
    /// <summary>Represents a loaded sprite.</summary>
    /// <param name="Textures">The loaded textures representing an animation.</param>
    /// <param name="FrameRate">The frame rate to play back the animation.</param>
    /// <param name="Loops">Whether the animation should loop.</param>
    [StructLayout(LayoutKind.Auto)]
    public readonly record struct Loaded(ImmutableArray<Texture2D> Textures, int FrameRate, bool Loops)
    {
        /// <summary>Gets the <see cref="Loaded"/> for <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">The type of sprites.</typeparam>
        /// <param name="manager">The content manager to load from.</param>
        /// <param name="x">The set of sprites to load.</param>
        /// <returns>The <see cref="Loaded"/> for <typeparamref name="T"/></returns>
        public static Loaded With<T>(ContentManager manager, T x)
            where T : struct, Enum =>
            typeof(T).FindPathToNull(x => x.DeclaringType)
               .Select(x => x.GetCustomAttribute<SpriteAttribute>())
                // ReSharper disable once NullableWarningSuppressionIsUsed
               .Aggregate(x.GetCustomAttribute<SpriteAttribute>()!, Join)
               .Load(manager);
    }

    /// <summary>Gets or sets whether the animation should loop.</summary>
    public bool Loops { get; init; }

    /// <summary>Gets or sets the number of frames.</summary>
    public int Frames { get; init; }

    /// <summary>Gets or sets the frame rate.</summary>
    public int FrameRate { get; init; }

    /// <summary>Gets the path.</summary>
    public string? Path => path;

    /// <summary>Joins two <see cref="SpriteAttribute"/> instances.</summary>
    /// <param name="accumulator">The accumulator.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The joined value.</returns>
    public static SpriteAttribute Join(SpriteAttribute accumulator, SpriteAttribute? next) =>
        new(Join(accumulator.Path, next?.Path))
        {
            Frames = Join(accumulator.Frames, next?.Frames),
            FrameRate = Join(accumulator.FrameRate, next?.FrameRate),
            Loops = accumulator.Loops || (next?.Loops ?? false),
        };

    /// <summary>Loads itself onto the provided <see cref="ContentManager"/>.</summary>
    /// <param name="manager">The content manager to load from.</param>
    /// <returns>The <see cref="Loaded"/> instance.</returns>
    public Loaded Load(ContentManager manager)
    {
        if (Frames is 0)
            return new([manager.Load<Texture2D>(Path)], FrameRate, Loops);

        var textures = new Texture2D[Frames];

        for (var i = 0; i < textures.Length; i++)
            textures[i] = manager.Load<Texture2D>($"{Path}/{i + 1}");

        return new(ImmutableCollectionsMarshal.AsImmutableArray(textures), FrameRate, Loops);
    }

    /// <inheritdoc cref="Join(SpriteAttribute, SpriteAttribute)"/>
    static int Join(int accumulator, int? next) => accumulator is 0 ? next ?? 0 : accumulator;

    /// <inheritdoc cref="Join(SpriteAttribute, SpriteAttribute)"/>
    static string? Join(string? accumulator, string? next) =>
        accumulator is null ? next :
        next is null ? accumulator : $"{next}/{accumulator}";
}
