using Silk.NET.Maths;
using Silk.NET.SDL;
using TheAdventure.Models;

namespace TheAdventure.Models;

public class GemObject : RenderableGameObject
{
    private const double CollectTime = 1.0;
    private double _elapsed = 0;
    private double _jumpVelocity = -120;
    private double _gravity = 320;
    private double _verticalOffset = 0;
    private bool _collected = false;

    public bool IsCollected => _collected;

    public GemObject(SpriteSheet spriteSheet, (int X, int Y) start)
        : base(spriteSheet, start)
    {
        SpriteSheet.ActivateAnimation("Idle");
    }

    public void Update(double msSinceLastFrame)
    {
        if (_collected)
        {
            return;
        }

        double dt = msSinceLastFrame / 1000.0;
        _elapsed += dt;

        _verticalOffset += _jumpVelocity * dt;
        _jumpVelocity += _gravity * dt;

        if (_verticalOffset > 0)
            _verticalOffset = 0;

        if (SpriteSheet.ActiveAnimation == null)
            SpriteSheet.ActivateAnimation("Idle");

        if (_elapsed >= CollectTime)
            _collected = true;
    }

    public override void Render(TheAdventure.GameRenderer renderer)
    {
        SpriteSheet.Render(renderer, (Position.X, Position.Y + (int)_verticalOffset));
    }
}