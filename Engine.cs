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

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    // Track previous state of bomb key (spacebar)
    private bool _bombKeyPrevState = false;

    // --- Crate spawn timer state ---
    private double _crateSpawnTimer = 0;
    private readonly Random _crateRandom = new();
    // ---

    // --- Coin state ---
    private readonly List<Coin> _coins = new();
    private int _totalCoins = 0;
    // ---

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        // Subscribe to attack event (left click)
        _input.OnAttack += (_, __) =>
        {
            if (_player != null)
            {
                _player.Attack();
            }
        };
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

        // Add some crates to the world (example positions)
        AddCrate(200, 200);
        AddCrate(300, 300);
        AddCrate(400, 200);

        // --- Initialize crate spawn timer ---
        ScheduleNextCrateSpawn();
        // ---

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

        _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);

        _scriptEngine.ExecuteAll(this);

        // --- Crate spawn timer logic ---
        UpdateCrateSpawn(msSinceLastFrame);
        // ---

        // --- Coin update logic ---
        UpdateCoins(msSinceLastFrame / 1000.0);
        // ---

        // Bomb placement: only on key down event (not held)
        bool bombKeyCurrent = _input.IsBombPressed();
        if (bombKeyCurrent && !_bombKeyPrevState)
        {
            AddBomb(_player.Position.X, _player.Position.Y, false);
        }
        _bombKeyPrevState = bombKeyCurrent;
    }

    // --- Crate spawn timer helpers ---
    private void UpdateCrateSpawn(double msSinceLastFrame)
    {
        _crateSpawnTimer -= msSinceLastFrame / 1000.0;
        if (_crateSpawnTimer <= 0)
        {
            SpawnRandomCrate();
            ScheduleNextCrateSpawn();
        }
    }

    private void ScheduleNextCrateSpawn()
    {
        // Next spawn in 5-10 seconds
        _crateSpawnTimer = 5.0 + _crateRandom.NextDouble() * 5.0;
    }

    private void SpawnRandomCrate()
    {
        if (_currentLevel == null || _currentLevel.Width == null || _currentLevel.Height == null ||
            _currentLevel.TileWidth == null || _currentLevel.TileHeight == null)
            return;

        int width = _currentLevel.Width.Value;
        int height = _currentLevel.Height.Value;
        int tileWidth = _currentLevel.TileWidth.Value;
        int tileHeight = _currentLevel.TileHeight.Value;

        // Avoid spawning on top of another crate
        int maxAttempts = 20;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int x = _crateRandom.Next(0, width) * tileWidth + tileWidth / 2;
            int y = _crateRandom.Next(0, height) * tileHeight + tileHeight / 2;

            bool overlaps = _gameObjects.Values
                .OfType<Crate>()
                .Any(crate => Math.Abs(crate.Position.X - x) < tileWidth && Math.Abs(crate.Position.Y - y) < tileHeight);

            if (!overlaps)
            {
                AddCrate(x, y);
                break;
            }
        }
    }
    // ---

    // --- Coin logic ---
    private void UpdateCoins(double deltaSeconds)
    {
        for (int i = _coins.Count - 1; i >= 0; i--)
        {
            var coin = _coins[i];
            coin.Update(deltaSeconds);
            if (coin.IsCollected)
            {
                _totalCoins += coin.Value;
                _coins.RemoveAt(i);
            }
        }
    }
    // ---

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        var playerPosition = _player!.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

        RenderTerrain();
        RenderAllObjects();
        RenderCoins();
        RenderCoinCounter();

        _renderer.PresentFrame();
    }

    public void RenderAllObjects()
    {
        var toRemove = new List<int>();

        // Find all bombs and crates
        var bombs = _gameObjects.Values.OfType<TemporaryGameObject>().ToList();
        var crates = _gameObjects.Values.OfType<Crate>().ToList();

        foreach (var gameObject in GetRenderables())
        {
            gameObject.Render(_renderer);

            if (gameObject is TemporaryGameObject { IsExpired: true } tempGameObject)
            {
                toRemove.Add(tempGameObject.Id);
            }
        }

        // Bomb/Crate collision detection
        foreach (var bomb in bombs)
        {
            if (bomb.IsExpired && !bomb.HasExploded)
            {
                foreach (var crate in crates)
                {
                    if (!crate.IsDestroyed)
                    {
                        // Simple collision: check if crate is within 48 pixels of bomb center
                        var dx = bomb.Position.X - crate.Position.X;
                        var dy = bomb.Position.Y - crate.Position.Y;
                        if (Math.Abs(dx) < 48 && Math.Abs(dy) < 48)
                        {
                            crate.Destroy();
                        }
                    }
                }
                bomb.HasExploded = true; // Mark as processed
            }
        }

        // Remove expired bombs and check for player death
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

        _player?.Render(_renderer);
    }

    private void RenderCoins()
    {
        foreach (var coin in _coins)
        {
            coin.Render(_renderer);
        }
    }

    private void RenderCoinCounter()
    {
        // Draw coin icon and total at top-left of the game window (screen coordinates)
        var coinSheet = SpriteSheet.Load(_renderer, "Coin.json", "Assets");
        coinSheet.ActivateAnimation("Idle");
        var frameWidth = coinSheet.FrameWidth;
        var frameHeight = coinSheet.FrameHeight;
        var sourceRect = new Rectangle<int>(0, 0, frameWidth, frameHeight);

        // Always draw at screen coordinates (8,8)
        var destRect = new Rectangle<int>(8, 8, frameWidth, frameHeight);
        _renderer.RenderTextureScreen(
            typeof(SpriteSheet)
                .GetField("_textureId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(coinSheet) as int? ?? -1,
            sourceRect,
            destRect
        );

        // Draw coin count as two digits (e.g., 00, 01, 12, etc.) next to the icon
        string coinText = _totalCoins.ToString("D2");
        _renderer.RenderText(coinText, 8 + frameWidth + 8, 8, 0xFFFFD700); // Gold color
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

        // Optionally, you can reduce the TTL for a snappier effect, e.g. 1.2 instead of 2.1
        TemporaryGameObject bomb = new(spriteSheet, 1.2, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }

    public void AddCrate(int X, int Y)
    {
        SpriteSheet crateSheet = SpriteSheet.Load(_renderer, "Crate.json", "Assets");
        Crate crate = new(crateSheet, (X, Y));
        crate.OnDestroyed += Crate_OnDestroyed; // Subscribe to drop coins
        _gameObjects.Add(crate.Id, crate);
    }

    private void Crate_OnDestroyed(Crate crate)
    {
        // Drop a coin at the crate's position, value 1-3
        int value = _crateRandom.Next(1, 4);
        var coinSheet = SpriteSheet.Load(_renderer, "Coin.json", "Assets");
        Coin coin = new(coinSheet, crate.Position, value);
        _coins.Add(coin);
    }
}