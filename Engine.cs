using System.Reflection;
using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly ScriptEngine _scriptEngine = new();

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private readonly List<ArrowObject> _arrows = new();
    private bool _canShootArrow = true;

    private readonly List<GemObject> _gems = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;
    private DateTimeOffset _nextOrcSpawn = DateTimeOffset.Now.AddSeconds(1);

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
    }

    public void SetupWorld()
    {
        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);

        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent);
        if (level == null)
        {
            throw new Exception("Failed to load level");
        }

        foreach (var tileSetRef in level.TileSets)
        {
            var tileSetContent = File.ReadAllText(Path.Combine("Assets", tileSetRef.Source));
            var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent);
            if (tileSet == null)
            {
                throw new Exception("Failed to load tile set");
            }

            foreach (var tile in tileSet.Tiles)
            {
                tile.TextureId = _renderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);
                _tileIdMap.Add(tile.Id!.Value, tile);
            }

            _loadedTileSets.Add(tileSet.Name, tileSet);
        }

        if (level.Width == null || level.Height == null)
        {
            throw new Exception("Invalid level dimensions");
        }

        if (level.TileWidth == null || level.TileHeight == null)
        {
            throw new Exception("Invalid tile dimensions");
        }

        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value));

        _currentLevel = level;

        _scriptEngine.LoadAll(Path.Combine("Assets", "Scripts"));
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        if (_player == null)
        {
            return;
        }

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;
        bool isAttacking = _input.IsAttackPressed() && (up + down + left + right <= 1);

        _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);
        if (isAttacking)
        {
            _player.Attack();
        }

        if (DateTimeOffset.Now > _nextOrcSpawn)
        {
            SpawnOrc();
            _nextOrcSpawn = DateTimeOffset.Now.AddSeconds(Random.Shared.Next(1, 4));
        }

        var orcIds = _gameObjects.Values.OfType<OrcObject>().Select(o => o.Id).ToList();
        foreach (var orcId in orcIds)
        {
            if (_gameObjects.TryGetValue(orcId, out var obj) && obj is OrcObject orc)
            {
                orc.Update(_player.Position, msSinceLastFrame, () =>
                {
                    _player.GameOver();
                });

                if (orc.IsDead && !orc.GemDropped)
                {
                    var gemSheet = SpriteSheet.Load(_renderer, "Gem.json", "Assets");
                    var gem = new GemObject(gemSheet, (orc.Position.X, orc.Position.Y - 16));
                    _gems.Add(gem);
                    orc.GemDropped = true;
                }

                if (orc.IsDead && orc.GemDropped)
                    _gameObjects.Remove(orc.Id);
            }
        }

        if (isAttacking)
        {
            foreach (var orc in _gameObjects.Values.OfType<OrcObject>())
            {
                var dx = orc.Position.X - _player.Position.X;
                var dy = orc.Position.Y - _player.Position.Y;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < 48 && !orc.IsDead)
                {
                    orc.TakeHit();
                }
            }
        }

        if (_input.IsLeftMouseJustPressed() && _canShootArrow)
        {
            var (mouseX, mouseY) = _input.GetMousePosition();
            var worldPos = _renderer.ToWorldCoordinates(mouseX, mouseY);
            var arrowSheet = SpriteSheet.Load(_renderer, "Arrow.json", "Assets");
            var arrow = new ArrowObject(arrowSheet, _player.Position, (worldPos.X, worldPos.Y));
            _arrows.Add(arrow);
            _canShootArrow = false;
        }
        if (!_input.IsLeftMouseJustPressed() && !_input.IsLeftPressed())
        {
            _canShootArrow = true;
        }

        foreach (var arrow in _arrows.ToList())
        {
            arrow.Update(msSinceLastFrame, _gameObjects.Values.OfType<OrcObject>());
            if (arrow.IsExpired)
                _arrows.Remove(arrow);
        }

        foreach (var gem in _gems.ToList())
        {
            gem.Update(msSinceLastFrame);
            if (gem.IsCollected)
                _gems.Remove(gem);
        }

        _scriptEngine.ExecuteAll(this);
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        var playerPosition = _player!.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

        RenderTerrain();
        RenderAllObjects();

        _renderer.PresentFrame();
    }

    public void RenderAllObjects()
    {
        var toRemove = new List<int>();
        foreach (var gameObject in GetRenderables())
        {
            gameObject.Render(_renderer);
            if (gameObject is TemporaryGameObject { IsExpired: true } tempGameObject)
            {
                toRemove.Add(tempGameObject.Id);
            }
        }

        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id, out var gameObject);

            if (_player == null)
            {
                continue;
            }

            var tempGameObject = (TemporaryGameObject)gameObject!;
            var deltaX = Math.Abs(_player.Position.X - tempGameObject.Position.X);
            var deltaY = Math.Abs(_player.Position.Y - tempGameObject.Position.Y);
            if (deltaX < 32 && deltaY < 32)
            {
                _player.GameOver();
            }
        }

        foreach (var arrow in _arrows)
        {
            arrow.Render(_renderer);
        }

        foreach (var gem in _gems)
        {
            gem.Render(_renderer);
        }

        _player?.Render(_renderer);
    }

    public void RenderTerrain()
    {
        foreach (var currentLayer in _currentLevel.Layers)
        {
            for (int i = 0; i < _currentLevel.Width; ++i)
            {
                for (int j = 0; j < _currentLevel.Height; ++j)
                {
                    int? dataIndex = j * currentLayer.Width + i;
                    if (dataIndex == null)
                    {
                        continue;
                    }

                    var currentTileId = currentLayer.Data[dataIndex.Value] - 1;
                    if (currentTileId == null)
                    {
                        continue;
                    }

                    var currentTile = _tileIdMap[currentTileId.Value];

                    var tileWidth = currentTile.ImageWidth ?? 0;
                    var tileHeight = currentTile.ImageHeight ?? 0;

                    var sourceRect = new Rectangle<int>(0, 0, tileWidth, tileHeight);
                    var destRect = new Rectangle<int>(i * tileWidth, j * tileHeight, tileWidth, tileHeight);
                    _renderer.RenderTexture(currentTile.TextureId, sourceRect, destRect);
                }
            }
        }
    }

    public IEnumerable<RenderableGameObject> GetRenderables()
    {
        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is RenderableGameObject renderableGameObject)
            {
                yield return renderableGameObject;
            }
        }
    }

    public (int X, int Y) GetPlayerPosition()
    {
        return _player!.Position;
    }

    public void AddBomb(int X, int Y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }

    private void SpawnOrc()
    {
        int minX = 0, minY = 0;
        int maxX = _currentLevel.Width!.Value * _currentLevel.TileWidth!.Value;
        int maxY = _currentLevel.Height!.Value * _currentLevel.TileHeight!.Value;

        int x = Random.Shared.Next(minX, maxX);
        int y = Random.Shared.Next(minY, maxY);

        var orcSheet = SpriteSheet.Load(_renderer, "Orc.json", "Assets");
        var orc = new OrcObject(orcSheet, (x, y));
        _gameObjects.Add(orc.Id, orc);
    }
}