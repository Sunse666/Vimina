namespace Vimina.Core.Input;

public class InputBuffer
{
    private string _buffer = "";

    public string Buffer => _buffer;

    public event EventHandler<string>? BufferChanged;
    public event EventHandler<string>? BufferConfirmed;
    public event EventHandler? BufferCleared;

    public void Append(char c)
    {
        _buffer += c;
        BufferChanged?.Invoke(this, _buffer);
    }

    public void Backspace()
    {
        if (_buffer.Length > 0)
        {
            _buffer = _buffer[..^1];
            BufferChanged?.Invoke(this, _buffer);
        }
    }

    public void Clear()
    {
        _buffer = "";
        BufferCleared?.Invoke(this, EventArgs.Empty);
    }

    public void Confirm()
    {
        if (!string.IsNullOrEmpty(_buffer))
        {
            BufferConfirmed?.Invoke(this, _buffer);
            Clear();
        }
    }
}
