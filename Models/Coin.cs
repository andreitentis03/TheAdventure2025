using Silk.NET.SDL;
using Silk.NET.Maths;

namespace TheAdventure.Models;

public class Coin : RenderableGameObject
{
    public int Value { get; }
    private double _elapsed;
    private double _landedElapsed;
    private bool _collected;
    private bool _landed;

    private readonly double _jumpDuration = 0.4;
    private readonly double _landDuration = 0.5;
    private readonly int _jumpHeight = 32;

    private readonly (int X, int Y) _startPos;
    private readonly (int X, int Y) _landPos;

    public bool IsCollected => _collected;

    public Coin(SpriteSheet spriteSheet, (int X, int Y) position, int value)
        : base(spriteSheet, position)
    {
        Value = value;
        _elapsed = 0;
        _landedElapsed = 0;
        _collected = false;
        _landed = false;
        _startPos = (position.X, position.Y - _jumpHeight);
        _landPos = position;
        SpriteSheet.ActivateAnimation("Spin");
    }

    public void Update(double deltaSeconds)
    {
        if (_collected)
            return;

        if (!_landed)
        {
            _elapsed += deltaSeconds;
            if (_elapsed >= _jumpDuration)
            {
                _landed = true;
                _landedElapsed = 0;
                SpriteSheet.ActivateAnimation("Idle");
                Position = _landPos;
            }
            else
            {
                double t = _elapsed / _jumpDuration;
                double jumpT = 1 - (1 - t) * (1 - t);
                int y = (int)(_startPos.Y + (_landPos.Y - _startPos.Y) * jumpT);
                Position = (_landPos.X, y);
            }
        }
        else
        {
            _landedElapsed += deltaSeconds;
            if (_landedElapsed >= _landDuration)
            {
                _collected = true;
            }
        }
    }

    public override void Render(GameRenderer renderer)
    {
        base.Render(renderer);
    }
}