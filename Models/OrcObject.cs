using Silk.NET.Maths;
using Silk.NET.SDL;
using TheAdventure.Models;

namespace TheAdventure.Models;

public class OrcObject : RenderableGameObject
{
    public enum OrcState
    {
        Idle,
        Walk,
        Attack,
        Hurt,
        Dead
    }

    private const int _speed = 90;
    private const int _attackRange = 12;
    private const int _attackCooldownMs = 1200;
    private const int _hurtAnimationMs = 400;
    private const int _deathAnimationMs = 600;

    public int Health { get; private set; } = 2;
    public OrcState State { get; private set; } = OrcState.Idle;
    private DateTimeOffset _lastAttack = DateTimeOffset.MinValue;
    private DateTimeOffset _hurtUntil = DateTimeOffset.MinValue;
    private DateTimeOffset _deadUntil = DateTimeOffset.MinValue;
    private bool _isAttacking = false;
    private bool _isDead = false;
    private int _lastMoveX = 1;

    public bool GemDropped { get; set; } = false;

    public OrcObject(SpriteSheet spriteSheet, (int X, int Y) position)
        : base(spriteSheet, position)
    {
        SetState(OrcState.Idle);
    }

    public void SetState(OrcState state)
    {
        if (_isDead && state != OrcState.Dead)
            return;

        State = state;
        switch (state)
        {
            case OrcState.Idle:
                SpriteSheet.ActivateAnimation("Idle");
                break;
            case OrcState.Walk:
                SpriteSheet.ActivateAnimation("Walk");
                break;
            case OrcState.Attack:
                SpriteSheet.ActivateAnimation("Attack");
                break;
            case OrcState.Hurt:
                SpriteSheet.ActivateAnimation("Hurt");
                break;
            case OrcState.Dead:
                SpriteSheet.ActivateAnimation("Dead");
                break;
        }
    }

    public void TakeHit()
    {
        if (_isDead) return;
        Health--;
        if (Health <= 0)
        {
            _isDead = true;
            _deadUntil = DateTimeOffset.Now.AddMilliseconds(_deathAnimationMs);
            SetState(OrcState.Dead);
        }
        else
        {
            _hurtUntil = DateTimeOffset.Now.AddMilliseconds(_hurtAnimationMs);
            SetState(OrcState.Hurt);
        }
    }

    public bool IsDead => _isDead && DateTimeOffset.Now > _deadUntil;

    public bool IsAttacking => _isAttacking;

    public void Update((int X, int Y) playerPos, double msSinceLastFrame, Action onAttackPlayer)
    {
        if (_isDead)
        {
            SetState(OrcState.Dead);
            return;
        }

        if (DateTimeOffset.Now < _hurtUntil)
        {
            SetState(OrcState.Hurt);
            return;
        }

        var dx = playerPos.X - Position.X;
        var dy = playerPos.Y - Position.Y;
        var dist = Math.Sqrt(dx * dx + dy * dy);

        if (Math.Abs(dx) > 1)
            _lastMoveX = dx > 0 ? 1 : -1;

        if (SpriteSheet.ActiveAnimation != null)
            SpriteSheet.ActiveAnimation.Flip = _lastMoveX < 0 ? RendererFlip.Horizontal : RendererFlip.None;

        if (dist <= _attackRange)
        {
            if (!_isAttacking && DateTimeOffset.Now > _lastAttack.AddMilliseconds(_attackCooldownMs))
            {
                _isAttacking = true;
                SetState(OrcState.Attack);
                _lastAttack = DateTimeOffset.Now;
                onAttackPlayer();
            }
            else if (SpriteSheet.AnimationFinished)
            {
                _isAttacking = false;
                SetState(OrcState.Idle);
            }
            return;
        }

        if (!_isAttacking && !_isDead && DateTimeOffset.Now >= _hurtUntil)
        {
            if (dist > 1)
            {
                SetState(OrcState.Walk);
                var move = _speed * (msSinceLastFrame / 1000.0);
                var nx = Position.X + (int)(move * dx / dist);
                var ny = Position.Y + (int)(move * dy / dist);
                Position = (nx, ny);
            }
            else
            {
                SetState(OrcState.Idle);
            }
        }
    }
}