// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Input;

/// <summary>Provides input from an input provider.</summary>
abstract partial class InputProvider : IDisposable, ISpanParsable<InputProvider>
{
    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MustDisposeResource] out InputProvider result
    ) =>
        (result = Parse(s.AsSpan(), provider)) is var _;

    /// <inheritdoc />
    public static bool TryParse(
        ReadOnlySpan<char> s,
        IFormatProvider? provider,
        [MustDisposeResource] out InputProvider result
    ) =>
        (result = Parse(s, provider)) is var _;

    /// <inheritdoc />
    [MustDisposeResource]
    public static InputProvider Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);

    /// <summary>Gets the default provider.</summary>
    /// <returns>The default provider.</returns>
    [MustDisposeResource]
    public static InputProvider Default()
    {
        if (new Evdev(out var warnings) is var e && warnings.All(x => x is WarningException or Win32Exception))
        {
            EscapeyGame.Log(warnings.AsSpan());
            return e;
        }

        e.Dispose();
        return new Sdl();
    }

    /// <inheritdoc />
    [MustDisposeResource]
    public static InputProvider Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (s.EqualsIgnoreCase(nameof(Sdl)))
            return new Sdl();

        if (!s.EqualsIgnoreCase(nameof(Evdev)))
            return Default();

        if (new Evdev(out var warnings) is var evdev && warnings.All(x => x is WarningException or Win32Exception))
        {
            EscapeyGame.Log(warnings.AsSpan());
            return evdev;
        }

        EscapeyGame.Log(warnings.AsSpan());
        evdev.Dispose();
        return new Sdl();
    }

    /// <summary>Clears all inputs.</summary>
    public abstract void Clear();

    /// <inheritdoc />
    public abstract void Dispose();

    /// <summary>Adds an input.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public abstract bool Add(Columns key, scoped ReadOnlySpan<char> value);

    /// <summary>Gets the valid values for <see cref="Add"/>.</summary>
    /// <returns>All valid values as one big comma-separated <see langword="string"/>.</returns>
    public abstract string GetValidValues();

    /// <inheritdoc />
    public override string ToString() => GetType().Name;

    /// <summary>Gets the next input.</summary>
    /// <returns>The next input.</returns>
    public abstract Columns Poll();
}
