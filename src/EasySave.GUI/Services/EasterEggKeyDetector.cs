using System;
using System.Diagnostics;
using Avalonia.Input;

namespace EasySave.GUI.Services;

/// <summary>
/// Detects a specific key sequence (6 → 7) typed anywhere in the application.
/// Fires <see cref="SequenceDetected"/> when the sequence is completed within the timeout.
/// </summary>
public sealed class EasterEggKeyDetector
{
    /// <summary>Maximum delay between the two key presses to form the sequence.</summary>
    private static readonly TimeSpan SequenceTimeout = TimeSpan.FromSeconds(2);

    private readonly Stopwatch _stopwatch = new();
    private bool _firstKeyPressed;

    /// <summary>Raised every time the key sequence is successfully completed.</summary>
    public event Action? SequenceDetected;

    /// <summary>
    /// Call this from a <see cref="KeyDown"/> handler.
    /// </summary>
    public void OnKeyDown(Key key)
    {
        if (key == Key.D6 || key == Key.NumPad6)
        {
            _firstKeyPressed = true;
            _stopwatch.Restart();
            return;
        }

        if ((key == Key.D7 || key == Key.NumPad7) && _firstKeyPressed)
        {
            if (_stopwatch.Elapsed <= SequenceTimeout)
            {
                _firstKeyPressed = false;
                _stopwatch.Reset();
                SequenceDetected?.Invoke();
                return;
            }
        }

        // Any other key (or timeout) resets the state.
        _firstKeyPressed = false;
        _stopwatch.Reset();
    }
}
