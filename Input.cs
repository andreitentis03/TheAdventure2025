using Silk.NET.SDL;

namespace TheAdventure;

public unsafe class Input
{
    private readonly Sdl _sdl;

    // Removed OnMouseClick event as mouse-based bomb spawning is disabled
    // public EventHandler<(int x, int y)>? OnMouseClick;

    public Input(Sdl sdl)
    {
        _sdl = sdl;
    }

    // WASD movement instead of arrow keys
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

    // Attack is now spacebar instead of 'A'
    public bool IsAttackPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.Space] == 1;
    }

    // Bomb spawning at player location is disabled, so always return false
    public bool IsKeyBPressed()
    {
        return false;
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
                        // Mouse-based bomb spawning is disabled, so do nothing
                        break;
                    }

                case (uint)EventType.Fingerup:
                    {
                        break;
                    }

                case (uint)EventType.Mousebuttonup:
                    {
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