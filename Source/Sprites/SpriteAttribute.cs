// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Sprites;

/// <summary>Associates the applied member with a <see cref="Sprite"/> or directory of sprites.</summary>
/// <param name="assetPath"></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Field)]
sealed class SpriteAttribute([UriString, StringSyntax(StringSyntaxAttribute.Uri)] string? assetPath = null) : Attribute
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
               .Aggregate(x.GetCustomAttribute<SpriteAttribute>()!, Join)
               .Load(manager);
    }

    /// <summary>Gets or sets whether the animation should loop.</summary>
    public bool Looping { get; init; }

    /// <summary>Gets or sets the frame rate.</summary>
    public int FrameRate { get; init; }

    /// <summary>Gets the path.</summary>
    public string? AssetPath => assetPath;

    /// <summary>Joins two <see cref="SpriteAttribute"/> instances.</summary>
    /// <param name="accumulator">The accumulator.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The joined value.</returns>
    public static SpriteAttribute Join(SpriteAttribute accumulator, SpriteAttribute? next) =>
        new(Join(accumulator.AssetPath, next?.AssetPath))
        {
            FrameRate = Join(accumulator.FrameRate, next?.FrameRate),
            Looping = accumulator.Looping || (next?.Looping ?? false),
        };

    /// <summary>Loads itself onto the provided <see cref="ContentManager"/>.</summary>
    /// <param name="manager">The content manager to load from.</param>
    /// <returns>The <see cref="Loaded"/> instance.</returns>
    public Loaded Load(ContentManager manager)
    {
        if (AssetExists(manager, AssetPath))
            return new([manager.Load<Texture2D>(AssetPath)], FrameRate, Looping);

        var textures = ImmutableArray.CreateBuilder<Texture2D>();

        for (var i = 1; $"{AssetPath}/{i}" is var path && AssetExists(manager, path); i++)
            textures.Add(manager.Load<Texture2D>(path));

        return textures is []
            ? throw new FileNotFoundException($"The following asset appears to be missing: {AssetPath}")
            : new(textures.DrainToImmutable(), FrameRate, Looping);
    }

    /// <summary>Determines if the path links to an asset.</summary>
    /// <param name="manager">The content manager.</param>
    /// <param name="path">The path to check.</param>
    /// <returns>Whether the parameter <paramref name="path"/> links to an asset.</returns>
    static bool AssetExists(ContentManager manager, [NotNullWhen(true)] string? path) =>
        path is not null &&
        Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? "", manager.RootDirectory, path) is var file &&
        Path.Exists(Path.ChangeExtension(file, "xnb"));

    /// <inheritdoc cref="Join(SpriteAttribute, SpriteAttribute)"/>
    static int Join(int accumulator, int? next) => accumulator is 0 ? next ?? 0 : accumulator;

    /// <inheritdoc cref="Join(SpriteAttribute, SpriteAttribute)"/>
    static string? Join(string? accumulator, string? next) =>
        accumulator is null ? next :
        next is null ? accumulator : $"{next}/{accumulator}";
}
