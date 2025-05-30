using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Models;
using Point = Silk.NET.SDL.Point;

namespace TheAdventure;

public unsafe class GameRenderer
{
    private Sdl _sdl;
    private Renderer* _renderer;
    private GameWindow _window;
    private Camera _camera;

    private Dictionary<int, IntPtr> _texturePointers = new();
    private Dictionary<int, TextureData> _textureData = new();
    private int _textureId;
    private int _fontTextureId = -1;

    public GameRenderer(Sdl sdl, GameWindow window)
    {
        _sdl = sdl;
        
        _renderer = (Renderer*)window.CreateRenderer();
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);
        
        _window = window;
        var windowSize = window.Size;
        _camera = new Camera(windowSize.Width, windowSize.Height);
    }

    public void SetWorldBounds(Rectangle<int> bounds)
    {
        _camera.SetWorldBounds(bounds);
    }

    public void CameraLookAt(int x, int y)
    {
        _camera.LookAt(x, y);
    }

    public int LoadTexture(string fileName, out TextureData textureInfo)
    {
        using (var fStream = new FileStream(fileName, FileMode.Open))
        {
            var image = Image.Load<Rgba32>(fStream);
            textureInfo = new TextureData()
            {
                Width = image.Width,
                Height = image.Height
            };
            var imageRAWData = new byte[textureInfo.Width * textureInfo.Height * 4];
            image.CopyPixelDataTo(imageRAWData.AsSpan());
            fixed (byte* data = imageRAWData)
            {
                var imageSurface = _sdl.CreateRGBSurfaceWithFormatFrom(data, textureInfo.Width,
                    textureInfo.Height, 8, textureInfo.Width * 4, (uint)PixelFormatEnum.Rgba32);
                if (imageSurface == null)
                {
                    throw new Exception("Failed to create surface from image data.");
                }
                
                var imageTexture = _sdl.CreateTextureFromSurface(_renderer, imageSurface);
                if (imageTexture == null)
                {
                    _sdl.FreeSurface(imageSurface);
                    throw new Exception("Failed to create texture from surface.");
                }
                
                _sdl.FreeSurface(imageSurface);
                
                _textureData[_textureId] = textureInfo;
                _texturePointers[_textureId] = (IntPtr)imageTexture;
            }
        }

        return _textureId++;
    }

    public void RenderTexture(int textureId, Rectangle<int> src, Rectangle<int> dst,
        RendererFlip flip = RendererFlip.None, double angle = 0.0, Point center = default)
    {
        if (_texturePointers.TryGetValue(textureId, out var imageTexture))
        {
            var translatedDst = _camera.ToScreenCoordinates(dst);
            _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, in src,
                in translatedDst,
                angle,
                in center, flip);
        }
    }

    public Vector2D<int> ToWorldCoordinates(int x, int y)
    {
        return _camera.ToWorldCoordinates(new Vector2D<int>(x, y));
    }

    public void SetDrawColor(byte r, byte g, byte b, byte a)
    {
        _sdl.SetRenderDrawColor(_renderer, r, g, b, a);
    }

    public void ClearScreen()
    {
        _sdl.RenderClear(_renderer);
    }

    public void PresentFrame()
    {
        _sdl.RenderPresent(_renderer);
    }

    public void RenderText(string text, int x, int y, uint color)
    {
        // Render only characters from the 5th row (row index 4), columns 0-9 of the bitmap font
        // Each character is 6x10 pixels, font image is "Assets/Font.png"
        // The 5th row, columns 0-9, are used for digits 0-9

        const int charWidth = 6;
        const int charHeight = 10;
        const int fontRow = 4; // 5th row (0-based index)
        const int fontColStart = 0;
        const int fontColEnd = 9;
        const int scale = 2; // Double the size
        const string fontPath = "Assets/Font.png";
        if (_fontTextureId == -1)
        {
            _fontTextureId = LoadTexture(fontPath, out _);
        }

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            // Only render digits 0-9
            if (c < '0' || c > '9')
                continue;

            int digit = c - '0';
            // Only use columns 0-9 in the 5th row
            if (digit < fontColStart || digit > fontColEnd)
                continue;

            var src = new Rectangle<int>(digit * charWidth, fontRow * charHeight, charWidth, charHeight);
            var dst = new Rectangle<int>(
                x + i * charWidth * scale,
                y,
                charWidth * scale,
                charHeight * scale
            );

            // Optionally, tint the font using color (not implemented here)
            RenderTextureScreen(_fontTextureId, src, dst);
        }
    }
    public void RenderTextureScreen(int textureId, Rectangle<int> src, Rectangle<int> dst,
    RendererFlip flip = RendererFlip.None, double angle = 0.0, Point center = default)
    {
        if (_texturePointers.TryGetValue(textureId, out var imageTexture))
        {
            // No camera transform: render directly to screen
            _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, in src,
                in dst,
                angle,
                in center, flip);
        }
    }
}
