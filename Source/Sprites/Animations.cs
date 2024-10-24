// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Sprites;

/// <summary>Maintains a list of <see cref="Animation{T}"/> instances.</summary>
/// <param name="game">The game that this component will belong to.</param>
/// <param name="width">The width of the render target.</param>
/// <param name="height">The height of the render target.</param>
sealed class Animations(Game game, int width, int height)
    : DrawableGameComponent(game), IReadOnlyList<DrawableGameComponent>
{
    /// <summary>Executes an action for each animation of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type of sprites.</typeparam>
    /// <typeparam name="TState">The type of the state.</typeparam>
    delegate void Act<in T, TState>(T a, ref TState state)
        where TState : allows ref struct;

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
        ForEach(ref gameTime, static (DrawableGameComponent c, ref GameTime t) => c.Draw(t));
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
        ForEach(ref value, static (Animation<T> a, ref T v) => a.Change(v));

    /// <summary>Changes all colors for animations of type <typeparamref name="T"/> with the given value.</summary>
    /// <returns>Itself.</returns>
    public Animations Colored<T>(Color color)
        where T : struct, Enum =>
        ForEach(ref color, static (Animation<T> a, ref Color v) => a.Colored(v));

    /// <inheritdoc cref="Change{T}(T)"/>
    public Animations Change<T>(T? value)
        where T : struct, Enum =>
        value is { } v ? Change(v) : this;

    /// <summary>Sets the Y position of all animations of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type of sprites.</typeparam>
    /// <param name="drawOrder">The new draw order position.</param>
    /// <returns>Itself.</returns>
    public Animations SetDrawOrder<T>(int drawOrder)
        where T : struct, Enum =>
        ForEach(ref drawOrder, static (Animation<T> a, ref int v) => a.DrawOrder = v);

    /// <summary>Sets the visibility of all animations of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type of sprites.</typeparam>
    /// <param name="visible">The new visibility.</param>
    /// <returns>Itself.</returns>
    public Animations SetVisibility<T>(bool visible)
        where T : struct, Enum =>
        ForEach(ref visible, static (Animation<T> a, ref bool v) => a.Visible = v);

    /// <summary>Syncs the animations.</summary>
    /// <typeparam name="T">The type of sprites.</typeparam>
    /// <typeparam name="TOther">The type of sprites.</typeparam>
    /// <returns>Itself.</returns>
    public Animations Sync<T, TOther>()
        where T : struct, Enum
        where TOther : struct, Enum
    {
        Animation<TOther>? v = default;
        ForEach(ref v, static (Animation<TOther> a, ref Animation<TOther>? v) => v = a);
        return ForEach(ref v, static (Animation<T> a, ref Animation<TOther>? v) => a.Sync(v));
    }

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

    /// <summary>Executes an action for each animation.</summary>
    /// <param name="x">The state to pass to the action.</param>
    /// <param name="a">The action to execute.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>Itself.</returns>
    [Inline] // ReSharper disable NullableWarningSuppressionIsUsed RedundantSuppressNullableWarningExpression
    void ForEach<T>(ref T x, [ResolveDelegate, RequireStaticDelegate(IsError = true)] Act<DrawableGameComponent, T> a)
        where T : allows ref struct
    {
        var animations = _animations.AsSpan();
        ref var start = ref MemoryMarshal.GetReference(animations);
        ref readonly var end = ref Unsafe.Add(ref start, animations.Length)!;

        for (; Unsafe.IsAddressLessThan(start, end); start = ref Unsafe.Add(ref start, 1)!)
            a(start, ref x);
    }

    /// <summary>Executes an action for each animation of type <typeparamref name="TA"/>.</summary>
    /// <param name="x">The state to pass to the action.</param>
    /// <param name="a">The action to execute.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <typeparam name="TA">The type of sprites.</typeparam>
    /// <returns>Itself.</returns>
    [Inline]
    Animations ForEach<T, TA>(ref T x, [ResolveDelegate, RequireStaticDelegate(IsError = true)] Act<Animation<TA>, T> a)
        where T : allows ref struct
        where TA : struct, Enum
    {
        if (Animation<TA>.Instance is { } instance)
        {
            a(instance, ref x);
            return this;
        }

        var animations = _animations.AsSpan();
        ref var start = ref MemoryMarshal.GetReference(animations);
        ref readonly var end = ref Unsafe.Add(ref start, animations.Length)!;

        for (; Unsafe.IsAddressLessThan(start, end); start = ref Unsafe.Add(ref start, 1)!)
            if (start is Animation<TA>)
                a(Unsafe.As<Animation<TA>>(start), ref x);

        return this;
    }
}
