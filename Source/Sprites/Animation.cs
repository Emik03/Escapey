// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Sprites;

/// <summary>Maintains a state machine for an animation.</summary>
/// <typeparam name="T">The type of sprites.</typeparam>
sealed class Animation<T> : DrawableGameComponent
    where T : struct, Enum
{
    /// <summary>The loaded sprites.</summary>
    readonly ImmutableArray<SpriteAttribute.Loaded> _sprites;

    /// <summary>The current state.</summary>
    int _frame, _index;

    /// <summary>The color to draw with.</summary>
    Color _color = Color.White;

    /// <summary>The synchronization function.</summary>
    Func<int>? _sync;

    /// <summary>The accumulated game time.</summary>
    TimeSpan _delta;

    /// <summary>Gets the current sprite.</summary>
    SpriteAttribute.Loaded CurrentSprite => _sprites[_index];

    /// <summary>Initializes a new instance of the <see cref="Animation{T}"/> class.</summary>
    /// <param name="game">The game that this component will belong to.</param>
    public Animation(Game game)
        : base(game)
    {
        var values = ImmutableCollectionsMarshal.AsImmutableArray(Enum.GetValues<T>());
        _sprites = ImmutableArray.CreateRange(values, (x, g) => SpriteAttribute.Loaded.With(g, x), game);
        _frame = LastFrame;
    }

    /// <summary>Gets or sets the minimum number of frames to wait before accepting a new state.</summary>
    public required int Min { get; init; }

    /// <summary>The sprite batch to draw with.</summary>
    public required SpriteBatch Batch { get; init; }

    /// <summary>Gets the size.</summary>
    public Point Size => _sprites[0].Textures is { IsDefault: false } and [var first, ..] ? first.Bounds.Size : default;

    /// <summary>Gets the number of frames of the current sprite.</summary>
    int FrameLength => CurrentSprite.Textures is { IsDefault: false, Length: var length } ? length : 0;

    /// <summary>Gets the last frame of the current sprite.</summary>
    int LastFrame => FrameLength - 1;

    /// <summary>Gets the current texture.</summary>
    Texture2D? CurrentTexture => CurrentSprite.Textures is { IsDefault: false } textures ? textures[_frame] : null;

    /// <inheritdoc />
    public override void Draw(GameTime time)
    {
        if (!Visible)
            return;

        if (_sync is not null)
            SetFrame(_sync());
        else if (CurrentSprite.FrameRate is not 0 and var frameRate)
        {
            var interval = TimeSpan.FromSeconds(1) / frameRate;
            _delta += time.ElapsedGameTime;

            if ((int)(_delta.Ticks / interval.Ticks) is > 0 and var advance)
            {
                _delta -= interval * advance;
                SetFrame(_frame + advance);
            }
        }

        if (CurrentTexture is { } texture)
            Batch.Draw(texture, new Vector2(0, DrawOrder), _color);
    }

    /// <summary>Changes the state.</summary>
    /// <param name="value">The new state.</param>
    /// <returns>Itself.</returns>
    public Animation<T> Change(T value)
    {
        if (value.AsInt() is var i && i < 0 || i >= _sprites.Length || i == _index || _frame < LastFrame.Min(Min))
            return this;

        _index = i;
        return Reset(true);
    }

    /// <inheritdoc cref="Change(T)"/>
    public Animation<T> Change(T? value) => value is { } v ? Change(v) : this;

    /// <summary>Changes the color.</summary>
    /// <returns>Itself.</returns>
    public Animation<T> Colored(Color color)
    {
        _color = color;
        return this;
    }

    /// <summary>Resets the animation back to the beginning.</summary>
    /// <returns>The </returns>
    public Animation<T> Reset(bool condition)
    {
        if (!condition)
            return this;

        _delta = TimeSpan.Zero;
        _frame = 0;
        return this;
    }

    /// <summary>Synchronizes the animation with another animation.</summary>
    /// <param name="other">The other animation to synchronize with.</param>
    /// <returns>Itself.</returns>
    public Animation<T> Sync<TOther>(Animation<TOther>? other)
        where TOther : struct, Enum
    {
        _sync = other is null ? null : other.GetFrame;
        return this;
    }

    /// <summary>Sets the current frame.</summary>
    /// <param name="value">The new frame.</param>
    void SetFrame(int value) => _frame = CurrentSprite.Loops ? value.Mod(FrameLength) : value.Min(LastFrame);

    /// <summary>Gets the current frame.</summary>
    /// <returns>The current frame.</returns>
    int GetFrame() => _frame;
}
