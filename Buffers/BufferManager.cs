using System.Collections.Generic;

namespace SayTheSpire2.Buffers;

public class BufferManager
{
    public static BufferManager Instance { get; } = new();

    private readonly List<Buffer> _buffers = new();
    private int _position = -1;

    public void Add(Buffer buffer)
    {
        _buffers.Add(buffer);
    }

    public Buffer? GetBuffer(string key)
    {
        foreach (var buffer in _buffers)
        {
            if (buffer.Key == key)
                return buffer;
        }
        return null;
    }

    public Buffer? CurrentBuffer
    {
        get
        {
            if (_position < 0 || _position >= _buffers.Count) return null;
            var buffer = _buffers[_position];
            return buffer.Enabled ? buffer : null;
        }
    }

    public void EnableBuffer(string key, bool enabled)
    {
        var buffer = GetBuffer(key);
        if (buffer == null) return;

        buffer.Enabled = enabled;

        if (!enabled && buffer == CurrentBuffer)
        {
            MoveToPrevious();
        }
        else if (enabled && _position == -1)
        {
            _position = _buffers.IndexOf(buffer);
            buffer.Update();
        }
    }

    public void SetCurrentBuffer(string key)
    {
        for (int i = 0; i < _buffers.Count; i++)
        {
            if (_buffers[i].Key == key)
            {
                _buffers[i].Update();
                _position = i;
                return;
            }
        }
    }

    public void DisableAll()
    {
        _position = -1;
        foreach (var buffer in _buffers)
            buffer.Enabled = false;
    }

    /// <summary>
    /// Disable all buffers except those in the always-enabled set.
    /// Used on focus changes to reset to the screen stack's baseline.
    /// </summary>
    public void ResetToAlwaysEnabled(HashSet<string> alwaysEnabled)
    {
        _position = -1;
        foreach (var buffer in _buffers)
            buffer.Enabled = alwaysEnabled.Contains(buffer.Key);
    }

    public bool MoveToNext()
    {
        if (_buffers.Count == 0) return false;

        int start = _position < 0 ? _buffers.Count - 1 : _position;
        int i = start;
        do
        {
            i++;
            if (i >= _buffers.Count) i = 0;
            if (_buffers[i].Enabled)
            {
                _position = i;
                _buffers[i].Update();
                return true;
            }
        } while (i != start);

        return false;
    }

    public bool MoveToPrevious()
    {
        if (_buffers.Count == 0) return false;

        int start = _position < 0 ? 0 : _position;
        int i = start;
        do
        {
            i--;
            if (i < 0) i = _buffers.Count - 1;
            if (_buffers[i].Enabled)
            {
                _position = i;
                _buffers[i].Update();
                return true;
            }
        } while (i != start);

        return false;
    }
}
