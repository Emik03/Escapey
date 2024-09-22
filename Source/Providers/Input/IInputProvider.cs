// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Providers.Input;

/// <summary>Provides input from an input provider.</summary>
partial interface IInputProvider : IDisposable
{
    /// <summary>Creates a default input provider.</summary>
    /// <returns>The default <see cref="IInputProvider"/>.</returns>
    [HandlesResourceDisposal]
    public static IInputProvider Default(out ImmutableArray<Exception> warnings)
    {
        if (new Evdev(out warnings) is var evdev && warnings.IsEmpty)
            return evdev;

        evdev.Dispose();
        warnings = [];
        return new Xna();
    }

    /// <summary>Creates an input provider from an alias.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="warnings">The warnings.</param>
    /// <returns>The <see cref="IInputProvider"/> from the parameter <paramref name="alias"/>.</returns>
    [HandlesResourceDisposal]
    public static IInputProvider FromAlias(scoped ReadOnlySpan<char> alias, out ImmutableArray<Exception> warnings)
    {
        if (!alias.EqualsIgnoreCase(nameof(Evdev)))
        {
            warnings = alias.IsWhiteSpace() || alias.EqualsIgnoreCase(nameof(Xna))
                ? []
                : [new FormatException($"Unrecognized alias, falling back to XNA: {alias}")];

            return new Xna();
        }

        if (!OperatingSystem.IsFreeBSD() && !OperatingSystem.IsLinux())
        {
            warnings = [new PlatformNotSupportedException("Evdev is unsupported, falling back to XNA.")];
            return new Xna();
        }

        var evdev = new Evdev(out var evdevWarnings);

        warnings = Environment.IsPrivilegedProcess
            ? evdevWarnings
            : [new WarningException("Evdev is not privileged, may not work on some input devices."), ..evdevWarnings];

        return evdev;
    }

    /// <summary>Adds an input.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>Whether the operation succeeded.</returns>
    bool Add(Columns key, scoped ReadOnlySpan<char> value);

    void Add<TSeparator, TStrategy>(
        Columns key,
        SplitSpan<char, TSeparator, TStrategy> values,
        ImmutableArray<Exception>.Builder accumulator
    )
    {
        foreach (var value in values)
            if (!Add(key, value.Trim()))
                accumulator.Add(new FormatException($"Unrecognized input, ignoring invalid value: {value}"));
    }

    /// <summary>Gets the next input.</summary>
    /// <returns>The next input.</returns>
    Columns Poll();
}
