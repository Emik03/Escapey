// SPDX-License-Identifier: LicenseRef-PolyForm-Strict
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
        if (new Evdev(out var eWarnings) is var evdev && eWarnings.All(x => x is WarningException or Win32Exception))
        {
            EscapeyGame.Log(eWarnings);
            return evdev;
        }

        evdev.Dispose();

        if (new User32(out var uWarnings) is var user && uWarnings.IsEmpty)
            return user;

        user.Dispose();
        return new Sdl();
    }

    /// <inheritdoc />
    [MustDisposeResource]
    public static InputProvider Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
#pragma warning disable IDISP017
        static InputProvider Evdev()
        {
            Evdev evdev = new(out var warnings);
            EscapeyGame.Log(warnings);

            if (warnings.All(x => x is WarningException or Win32Exception))
                return evdev;

            evdev.Dispose();
            return new Sdl();
        }

        static InputProvider User32()
        {
            User32 user32 = new(out var warnings);
            EscapeyGame.Log(warnings);

            if (warnings.IsEmpty)
                return user32;

            user32.Dispose();
            return new Sdl();
        }
#pragma warning restore IDISP017
        return 0 switch
        {
            _ when s.EqualsIgnoreCase(nameof(Sdl)) => new Sdl(),
            _ when s.EqualsIgnoreCase(nameof(Evdev)) => Evdev(),
            _ when s.EqualsIgnoreCase(nameof(User32)) => User32(),
            _ => Default(),
        };
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
