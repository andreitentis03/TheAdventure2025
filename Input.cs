using Silk.NET.SDL;

namespace TheAdventure;

public unsafe class Input
{
    private readonly Sdl _sdl;

    private bool _leftMouseDown = false;
    private bool _leftMouseJustPressed = false;
    public Input(Sdl sdl)
    {
        _sdl = sdl;
    }

    public bool IsLeftPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.A] == 1;
    }

    public bool IsRightPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.D] == 1;
    }

    public bool IsUpPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.W] == 1;
    }

    public bool IsDownPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.S] == 1;
    }

    public bool IsAttackPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.Space] == 1;
    }

    public bool IsKeyBPressed()
    {
        return false;
    }

    public bool IsLeftMouseJustPressed()
    {
        var result = _leftMouseJustPressed;
        _leftMouseJustPressed = false;
        return result;
    }

    public (int X, int Y) GetMousePosition()
    {
        int x = 0, y = 0;
        _sdl.GetMouseState(&x, &y);
        return (x, y);
    }

    public bool ProcessInput()
    {
        Event ev = new Event();
        while (_sdl.PollEvent(ref ev) != 0)
        {
            if (ev.Type == (uint)EventType.Quit)
            {
                return true;
            }

            switch (ev.Type)
            {
                case (uint)EventType.Windowevent:
                    {
                        switch (ev.Window.Event)
                        {
                            case (byte)WindowEventID.Shown:
                            case (byte)WindowEventID.Exposed:
                                {
                                    break;
                                }
                            case (byte)WindowEventID.Hidden:
                                {
                                    break;
                                }
                            case (byte)WindowEventID.Moved:
                                {
                                    break;
                                }
                            case (byte)WindowEventID.SizeChanged:
                                {
                                    break;
                                }
                            case (byte)WindowEventID.Minimized:
                            case (byte)WindowEventID.Maximized:
                            case (byte)WindowEventID.Restored:
                                break;
                            case (byte)WindowEventID.Enter:
                                {
                                    break;
                                }
                            case (byte)WindowEventID.Leave:
                                {
                                    break;
                                }
                            case (byte)WindowEventID.FocusGained:
                                {
                                    break;
                                }
                            case (byte)WindowEventID.FocusLost:
                                {
                                    break;
                                }
                            case (byte)WindowEventID.Close:
                                {
                                    break;
                                }
                            case (byte)WindowEventID.TakeFocus:
                                {
                                    _sdl.SetWindowInputFocus(_sdl.GetWindowFromID(ev.Window.WindowID));
                                    break;
                                }
                        }

                        break;
                    }

                case (uint)EventType.Fingermotion:
                    {
                        break;
                    }

                case (uint)EventType.Mousemotion:
                    {
                        break;
                    }

                case (uint)EventType.Fingerdown:
                    {
                        break;
                    }
                case (uint)EventType.Mousebuttondown:
                    {
                        if (ev.Button.Button == (byte)MouseButton.Primary)
                        {
                            if (!_leftMouseDown)
                            {
                                _leftMouseJustPressed = true;
                            }
                            _leftMouseDown = true;
                        }
                        break;
                    }

                case (uint)EventType.Fingerup:
                    {
                        break;
                    }

                case (uint)EventType.Mousebuttonup:
                    {
                        if (ev.Button.Button == (byte)MouseButton.Primary)
                        {
                            _leftMouseDown = false;
                        }
                        break;
                    }

                case (uint)EventType.Mousewheel:
                    {
                        break;
                    }

                case (uint)EventType.Keyup:
                    {
                        break;
                    }

                case (uint)EventType.Keydown:
                    {
                        break;
                    }
            }
        }

        return false;
    }
}