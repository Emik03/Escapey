// SPDX-License-Identifier: MPL-2.0
namespace Escapey.Domains;

/// <summary>Represents the toggled state.</summary>
/// <param name="start">The starting state.</param>
struct Toggle(bool start)
{
    /// <summary>The state of the button.</summary>
    byte _state = (byte)(start.ToByte() * 2);

    /// <summary>Gets the state.</summary>
    /// <param name="toggle">The toggle.</param>
    /// <returns>The state.</returns>
    public static bool operator false(Toggle toggle) => toggle._state is 0 or 3;

    /// <summary>Gets the state.</summary>
    /// <param name="toggle">The toggle.</param>
    /// <returns>The state.</returns>
    public static bool operator true(Toggle toggle) => toggle._state is 1 or 2;

    /// <summary>Converts the held state to the toggled state.</summary>
    /// <param name="value">The starting state.</param>
    /// <returns>The toggled state.</returns>
    public static implicit operator Toggle(bool value) => new(value);

    /// <summary>Converts the held state to the toggled state.</summary>
    /// <param name="value">The held state.</param>
    /// <returns>The toggled state.</returns>
    public bool Accept(bool value) =>
        _state switch
        {
            0 => value && (_state = 1) is var _,
            1 => value || (_state = 2) is var _,
            2 => !(value && (_state = 3) is var _),
            _ => !(value || (_state = 0) is var _),
        };
}
