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
        /// <param name="game">The game to load for.</param>
        /// <param name="x">The set of sprites to load.</param>
        /// <returns>The <see cref="Loaded"/> for <typeparamref name="T"/></returns>
        public static Loaded With<T>(Game game, T x)
            where T : struct, Enum =>
            typeof(T).FindPathToNull(x => x.DeclaringType)
               .Select(x => x.GetCustomAttribute<SpriteAttribute>())
               .Aggregate(x.GetCustomAttribute<SpriteAttribute>()!, Join)
               .Load(game);
    }

    /// <summary>Gets or sets whether the animation should loop.</summary>
    public bool Looping { get; init; }

    /// <summary>Gets or sets the frame rate.</summary>
    public int FrameRate { get; init; }

    /// <summary>Gets the path.</summary>
    public string? AssetPath => assetPath;

    /// <summary>Gets the full path.</summary>
    /// <param name="game">The game.</param>
    /// <param name="path">The path.</param>
    /// <returns>The full path.</returns>
    public static string GetFullPath(Game game, string path) =>
        Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? "", game.Content.RootDirectory, path);

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

    /// <summary>Runs the delegate in a context where <see cref="ContentManager.RootDirectory"/> is empty.</summary>
    /// <typeparam name="T">The resulting type of the delegate.</typeparam>
    /// <param name="game">The game.</param>
    /// <param name="converter">The delegate to call.</param>
    /// <returns>The value returned from the parameter <paramref name="converter"/>.</returns>
    public static T FromRoot<T>(Game game, [InstantHandle] Converter<Game, T> converter)
    {
        var root = game.Content.RootDirectory;
        game.Content.RootDirectory = "";
        var ret = converter(game);
        game.Content.RootDirectory = root;
        return ret;
    }

    /// <summary>Loads itself onto the provided <see cref="ContentManager"/>.</summary>
    /// <param name="game">The content manager to load from.</param>
    /// <returns>The <see cref="Loaded"/> instance.</returns>
    public Loaded Load(Game game) =>
        TryLoad(game) ?? FromRoot(game, TryLoad) ?? throw new FileNotFoundException($"Missing sprite: {AssetPath}");

    /// <summary>Determines if the path links to an asset.</summary>
    /// <param name="game">The game.</param>
    /// <param name="path">The path to check.</param>
    /// <returns>Whether the parameter <paramref name="path"/> links to an asset.</returns>
    static Texture2D? FromDisk(Game game, [NotNullWhen(true)] string? path)
    {
        static string? WithExtension(string path, string extension) =>
            Path.ChangeExtension(path, extension) is var file && Path.Exists(file) ? file : null;

        if (path is null)
            return null;

        if (GetFullPath(game, path) is var file && WithExtension(file, "xnb") is not null)
            return game.Content.Load<Texture2D>(file);

        ReadOnlySpan<string> extensions = ["bmp", "dis", "gif", "jpeg", "jpg", "png", "tif"];

        foreach (var extension in extensions)
            if (WithExtension(file, extension) is { } fileWithExtension)
                return Texture2D.FromFile(game.GraphicsDevice, fileWithExtension);

        return null;
    }

    /// <inheritdoc cref="Join(SpriteAttribute, SpriteAttribute)"/>
    static int Join(int accumulator, int? next) => accumulator is 0 ? next ?? 0 : accumulator;

    /// <inheritdoc cref="Join(SpriteAttribute, SpriteAttribute)"/>
    static string? Join(string? accumulator, string? next) =>
        accumulator is null ? next :
        next is null ? accumulator : $"{next}/{accumulator}";

    /// <summary>Attempts to load the asset.</summary>
    /// <param name="game">The game.</param>
    /// <returns>The <see cref="Loaded"/> instance, if it succeeds.</returns>
    Loaded? TryLoad(Game game) =>
        FromDisk(game, AssetPath) is { } still ? new([still], FrameRate, Looping) :
        Enumerable.Range(1, Array.MaxLength)
           .Select(x => FromDisk(game, $"{AssetPath}/{x}"))
           .TakeUntil(x => x is null)
           .Filter()
           .ToImmutableArray() is var textures and not [] ? new(textures, FrameRate, Looping) : null;
}
