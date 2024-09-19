// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Sprites;

/// <summary>Maintains a list of <see cref="Animation{T}"/> instances.</summary>
/// <param name="game">The game that this component will belong to.</param>
sealed class Animations(Game game, int width, int height)
    : DrawableGameComponent(game), IReadOnlyList<DrawableGameComponent>
{
    /// <summary>The list of animations.</summary>
    readonly List<DrawableGameComponent> _animations = [];

    /// <summary>The render target to draw to.</summary>
    readonly RenderTarget2D _target = new(game.GraphicsDevice, width, height);

    /// <summary>The background color.</summary>
    Color _background;

    /// <inheritdoc />
    public DrawableGameComponent this[int index] => _animations[index];

    /// <inheritdoc />
    public int Count => _animations.Count;

    /// <summary>The sprite batch to draw with.</summary>
    public SpriteBatch Batch { get; } = new(game.GraphicsDevice);

    /// <inheritdoc />
    public override void Draw(GameTime gameTime)
    {
        if (!Visible)
            return;

        GraphicsDevice.SetRenderTarget(_target);
        GraphicsDevice.Clear(_background);
        Batch.Begin();
        ForEach(gameTime, static (DrawableGameComponent c, GameTime t) => c.Draw(t));
        Batch.End();
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(_background);
        Batch.Begin();
        var resolution = GraphicsDevice.Resolution(width, height);
        Batch.Draw(_target, resolution, Color.White);
        Batch.End();
    }

    /// <summary>Adds an animation.</summary>
    /// <typeparam name="T">The type of sprites.</typeparam>
    /// <param name="value">The value of the animation, as well as the starting state.</param>
    /// <param name="min">The minimum number of frames to wait before accepting a new state.</param>
    /// <param name="visible">Whether to start the animation visible.</param>
    /// <returns>Itself.</returns>
    public Animations Add<T>(T value = default, int min = 0, bool visible = true)
        where T : struct, Enum
    {
        _animations.Add(new Animation<T>(Game) { Batch = Batch, Min = min, Visible = visible }.Change(value));
        return this;
    }

    /// <summary>Sets the background color.</summary>
    /// <returns>Itself.</returns>
    public Animations Background(Color background)
    {
        _background = background;
        return this;
    }

    /// <summary>Changes all states for animations of type <typeparamref name="T"/> with the given value.</summary>
    /// <typeparam name="T">The type of sprites.</typeparam>
    /// <param name="value">The new state.</param>
    /// <returns>Itself.</returns>
    public Animations Change<T>(T value)
        where T : struct, Enum =>
        ForEach(value, static (Animation<T> a, T v) => a.Change(v));

    /// <summary>Changes all colors for animations of type <typeparamref name="T"/> with the given value.</summary>
    /// <returns>Itself.</returns>
    public Animations Colored<T>(Color color)
        where T : struct, Enum =>
        ForEach(color, static (Animation<T> a, Color v) => a.Colored(v));

    /// <inheritdoc cref="Change{T}(T)"/>
    public Animations Change<T>(T? value)
        where T : struct, Enum =>
        value is { } v ? Change(v) : this;

    /// <summary>Resets the animation from the start.</summary>
    /// <typeparam name="T">The type of sprites.</typeparam>
    /// <param name="reset">Whether to reset the animation.</param>
    /// <returns>Itself.</returns>
    public Animations Reset<T>(bool reset)
        where T : struct, Enum =>
        ForEach(reset, static (Animation<T> a, bool v) => a.Reset(v));

    /// <summary>Sets the visibility of all animations of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type of sprites.</typeparam>
    /// <param name="visible">The new visibility.</param>
    /// <returns>Itself.</returns>
    public Animations SetVisibility<T>(bool visible)
        where T : struct, Enum =>
        ForEach(visible, static (Animation<T> a, bool v) => a.Visible = v);

    /// <inheritdoc />
    public IEnumerator<DrawableGameComponent> GetEnumerator() => _animations.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Batch.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>Executes an action for each animation of type <typeparamref name="TAnimation"/>.</summary>
    /// <param name="state">The state to pass to the action.</param>
    /// <param name="action">The action to execute.</param>
    /// <typeparam name="TAnimation">The type of sprites.</typeparam>
    /// <typeparam name="TState">The type of the state.</typeparam>
    /// <returns>Itself.</returns>
    [Inline]
    Animations ForEach<TAnimation, TState>(
        TState state,
        [RequireStaticDelegate(IsError = true)] Action<TAnimation, TState> action
    )
        where TAnimation : DrawableGameComponent
    {
        var animations = _animations.AsSpan();
        ref var start = ref MemoryMarshal.GetReference(animations);
        ref readonly var end = ref Unsafe.Add(ref start, animations.Length);

        for (; Unsafe.IsAddressLessThan(start, end); start = ref Unsafe.Add(ref start, 1))
            if (start is TAnimation a)
                action(a, state);

        return this;
    }
}
