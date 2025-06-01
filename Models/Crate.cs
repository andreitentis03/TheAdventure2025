using Silk.NET.SDL;

namespace TheAdventure.Models;

public class Crate : RenderableGameObject
{
    public bool IsDestroyed { get; private set; }
    private bool _breakAnimationStarted = false;
    private bool _breakAnimationFinished = false;

    public event Action<Crate>? OnDestroyed; // NEW

    public Crate(SpriteSheet spriteSheet, (int X, int Y) position)
        : base(spriteSheet, position)
    {
        IsDestroyed = false;
        SpriteSheet.ActivateAnimation("Idle");
    }

    public void Destroy()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;
        _breakAnimationStarted = true;
        SpriteSheet.ActivateAnimation("Break");
        OnDestroyed?.Invoke(this);
    }

    public override void Render(GameRenderer renderer)
    {
        if (IsDestroyed && _breakAnimationStarted && SpriteSheet.AnimationFinished)
        {
            var frameWidth = SpriteSheet.FrameWidth;
            var frameHeight = SpriteSheet.FrameHeight;
            var dest = (Position.X, Position.Y);
            var sourceRect = new Silk.NET.Maths.Rectangle<int>(6 * frameWidth, 0, frameWidth, frameHeight);
            var destRect = new Silk.NET.Maths.Rectangle<int>(
                dest.X - SpriteSheet.FrameCenter.OffsetX,
                dest.Y - SpriteSheet.FrameCenter.OffsetY,
                frameWidth, frameHeight
            );
            renderer.RenderTexture(
                typeof(SpriteSheet)
                    .GetField("_textureId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .GetValue(SpriteSheet) as int? ?? -1,
                sourceRect,
                destRect
            );
            _breakAnimationStarted = false;
            _breakAnimationFinished = true;
            return;
        }

        base.Render(renderer);
    }
}