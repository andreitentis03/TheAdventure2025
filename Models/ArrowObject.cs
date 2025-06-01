using Silk.NET.Maths;
using TheAdventure.Models;

public class ArrowObject : RenderableGameObject
{
    private readonly double _speed = 400;
    private readonly double _lifetime = 3.0;
    private double _elapsed = 0;
    private readonly double _vx, _vy;
    private bool _hit = false;

    public ArrowObject(SpriteSheet spriteSheet, (int X, int Y) start, (int X, int Y) target)
        : base(spriteSheet, start)
    {
        var dx = target.X - start.X;
        var dy = target.Y - start.Y;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist == 0) dist = 1;
        _vx = dx / dist;
        _vy = dy / dist;

        Position = (start.X, start.Y);

        Angle = Math.Atan2(dy, dx) * (180.0 / Math.PI);

        RotationCenter = new Silk.NET.SDL.Point
        {
            X = spriteSheet.FrameCenter.OffsetX,
            Y = spriteSheet.FrameCenter.OffsetY
        };
    }

    public bool IsExpired => _elapsed >= _lifetime || _hit;

    public void Update(double msSinceLastFrame, IEnumerable<OrcObject> orcs)
    {
        if (_hit) return;

        var move = _speed * (msSinceLastFrame / 1000.0);
        Position = ((int)(Position.X + _vx * move), (int)(Position.Y + _vy * move));
        _elapsed += msSinceLastFrame / 1000.0;

        foreach (var orc in orcs)
        {
            if (orc.IsDead) continue;
            var dx = orc.Position.X - Position.X;
            var dy = orc.Position.Y - Position.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 40)
            {
                orc.TakeHit();
                _hit = true;
                break;
            }
        }
    }
}