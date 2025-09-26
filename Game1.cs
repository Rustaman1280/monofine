using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Monogame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private FirstPersonCamera _camera = null!;
    private BasicEffect _effect = null!;
    private SpriteFont _font = null!;
    private Texture2D _crosshairTexture = null!;

    private VertexPositionColor[] _floorVertices = Array.Empty<VertexPositionColor>();
    private short[] _floorIndices = Array.Empty<short>();
    private VertexPositionColor[] _cubeVertices = Array.Empty<VertexPositionColor>();
    private short[] _cubeIndices = Array.Empty<short>();

    private readonly List<Enemy> _enemies = new();
    private readonly Random _random = new();

    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private bool _suppressNextMouseDelta = true;
    private float _fireCooldown;
    private int _score;

    private const float MouseSensitivity = 0.0025f;
    private const float MovementSpeed = 6f;
    private const float SprintMultiplier = 1.8f;
    private const float FireDelaySeconds = 0.25f;
    private const float EnemySize = 1.2f;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;
    }

    protected override void Initialize()
    {
        Window.AllowUserResizing = false;
        Window.Title = "MonoGame FPS Demo";

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        _camera = new FirstPersonCamera(GraphicsDevice.Viewport.AspectRatio);
        _camera.SetPosition(new Vector3(0f, 0f, 8f));

        base.Initialize();

        CenterMouse();
        _previousMouse = Mouse.GetState();
        _previousKeyboard = Keyboard.GetState();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _effect = new BasicEffect(GraphicsDevice)
        {
            LightingEnabled = false,
            TextureEnabled = false,
            VertexColorEnabled = true
        };

        _font = Content.Load<SpriteFont>("Default");

        CreateFloorGeometry();
        CreateCubeGeometry();

        _crosshairTexture = CreateCrosshairTexture(GraphicsDevice, 32, 3, 9);

        InitializeEnemies(5);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        var deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _fireCooldown = MathF.Max(0f, _fireCooldown - deltaSeconds);

        HandleInput(deltaSeconds);

        _previousMouse = Mouse.GetState();
        _previousKeyboard = Keyboard.GetState();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.BlendState = BlendState.Opaque;

        _effect.View = _camera.View;
        _effect.Projection = _camera.Projection;

        DrawFloor();
        DrawEnemies();

        GraphicsDevice.DepthStencilState = DepthStencilState.None;

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        DrawHud();
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void HandleInput(float deltaSeconds)
    {
        if (!IsActive)
        {
            _suppressNextMouseDelta = true;
            return;
        }

        var mouseState = Mouse.GetState();
        var keyboardState = Keyboard.GetState();

        HandleMouse(mouseState);
        HandleMovement(keyboardState, deltaSeconds);
        HandleFire(mouseState, keyboardState);

        CenterMouse();
    }

    private void HandleMouse(MouseState mouseState)
    {
        var viewport = GraphicsDevice.Viewport;
        var center = new Point(viewport.Width / 2, viewport.Height / 2);
        var delta = new Point(mouseState.X - center.X, mouseState.Y - center.Y);

        if (_suppressNextMouseDelta)
        {
            _suppressNextMouseDelta = false;
            return;
        }

        if (delta.X != 0 || delta.Y != 0)
        {
            _camera.Rotate(delta.X * MouseSensitivity, delta.Y * MouseSensitivity);
        }
    }

    private void HandleMovement(KeyboardState keyboardState, float deltaSeconds)
    {
        Vector3 movement = Vector3.Zero;

        var forward = _camera.ForwardOnPlane;
        var right = _camera.RightOnPlane;

        if (forward.LengthSquared() > 0f)
        {
            if (keyboardState.IsKeyDown(Keys.W))
                movement += forward;
            if (keyboardState.IsKeyDown(Keys.S))
                movement -= forward;
        }

        if (right.LengthSquared() > 0f)
        {
            if (keyboardState.IsKeyDown(Keys.D))
                movement += right;
            if (keyboardState.IsKeyDown(Keys.A))
                movement -= right;
        }

        if (movement.LengthSquared() > 0f)
        {
            movement.Normalize();
            var speed = MovementSpeed * (keyboardState.IsKeyDown(Keys.LeftShift) ? SprintMultiplier : 1f);
            _camera.Move(movement * speed * deltaSeconds);
        }
    }

    private void HandleFire(MouseState mouseState, KeyboardState keyboardState)
    {
        var isFiring = mouseState.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
        var isUsingSpace = keyboardState.IsKeyDown(Keys.Space) && !_previousKeyboard.IsKeyDown(Keys.Space);

        if (_fireCooldown > 0f)
        {
            return;
        }

        if (isFiring || isUsingSpace)
        {
            if (TryShoot())
            {
                _score++;
            }

            _fireCooldown = FireDelaySeconds;
        }
    }

    private bool TryShoot()
    {
        var ray = new Ray(_camera.Position, _camera.Forward);
        Enemy closestEnemy = null!;
        var closestDistance = float.MaxValue;
        var foundEnemy = false;

        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive)
                continue;

            var intersection = enemy.Bounds.Intersects(ray);
            if (!intersection.HasValue)
                continue;

            if (intersection.Value < closestDistance)
            {
                closestDistance = intersection.Value;
                closestEnemy = enemy;
                foundEnemy = true;
            }
        }

        if (!foundEnemy)
        {
            return false;
        }

        closestEnemy.Kill();
        RespawnEnemy(closestEnemy);
        return true;
    }

    private void InitializeEnemies(int count)
    {
        _enemies.Clear();
        for (var i = 0; i < count; i++)
        {
            var enemy = new Enemy(EnemySize);
            _enemies.Add(enemy);
            RespawnEnemy(enemy);
        }
    }

    private void RespawnEnemy(Enemy enemy)
    {
        Vector3 position;
        const float minDistance = 6f;
        const float maxRange = 25f;

        do
        {
            var x = MathHelper.Lerp(-maxRange, maxRange, (float)_random.NextDouble());
            var z = MathHelper.Lerp(-maxRange, maxRange, (float)_random.NextDouble());
            position = new Vector3(x, EnemySize * 0.5f, z);
        }
        while (Vector2.DistanceSquared(new Vector2(position.X, position.Z), new Vector2(_camera.Position.X, _camera.Position.Z)) < minDistance * minDistance);

        var palette = new[]
        {
            Color.Crimson,
            Color.DarkOrange,
            Color.Gold,
            Color.Cyan,
            Color.MediumPurple,
            Color.Chartreuse
        };

        var color = palette[_random.Next(palette.Length)];
        enemy.Respawn(position, color);
    }

    private void CreateFloorGeometry()
    {
        const float halfSize = 32f;
        const float y = 0f;
        var baseColor = new Color(56, 84, 63);
        var edgeColor = new Color(70, 112, 80);

        _floorVertices = new[]
        {
            new VertexPositionColor(new Vector3(-halfSize, y, -halfSize), baseColor),
            new VertexPositionColor(new Vector3(halfSize, y, -halfSize), edgeColor),
            new VertexPositionColor(new Vector3(halfSize, y, halfSize), baseColor),
            new VertexPositionColor(new Vector3(-halfSize, y, halfSize), edgeColor)
        };

        _floorIndices = new short[] { 0, 1, 2, 0, 2, 3 };
    }

    private void CreateCubeGeometry()
    {
        var half = EnemySize * 0.5f;

        _cubeVertices = new[]
        {
            new VertexPositionColor(new Vector3(-half, -half, -half), Color.White),
            new VertexPositionColor(new Vector3(-half, half, -half), Color.White),
            new VertexPositionColor(new Vector3(half, half, -half), Color.White),
            new VertexPositionColor(new Vector3(half, -half, -half), Color.White),
            new VertexPositionColor(new Vector3(-half, -half, half), Color.White),
            new VertexPositionColor(new Vector3(-half, half, half), Color.White),
            new VertexPositionColor(new Vector3(half, half, half), Color.White),
            new VertexPositionColor(new Vector3(half, -half, half), Color.White)
        };

        _cubeIndices = new short[]
        {
            0, 1, 2, 0, 2, 3,
            4, 6, 5, 4, 7, 6,
            4, 5, 1, 4, 1, 0,
            3, 2, 6, 3, 6, 7,
            1, 5, 6, 1, 6, 2,
            4, 0, 3, 4, 3, 7
        };
    }

    private void DrawFloor()
    {
        _effect.World = Matrix.Identity;
        _effect.DiffuseColor = Vector3.One;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _floorVertices, 0, 4, _floorIndices, 0, 2);
        }
    }

    private void DrawEnemies()
    {
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive)
                continue;

            var world = Matrix.CreateTranslation(enemy.Position);
            _effect.World = world;
            _effect.DiffuseColor = enemy.Color.ToVector3();

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _cubeVertices, 0, 8, _cubeIndices, 0, _cubeIndices.Length / 3);
            }
        }
    }

    private void DrawHud()
    {
        var viewport = GraphicsDevice.Viewport;
        var center = new Vector2(viewport.Width * 0.5f, viewport.Height * 0.5f);

        if (_crosshairTexture != null)
        {
            var crosshairOrigin = new Vector2(_crosshairTexture.Width * 0.5f, _crosshairTexture.Height * 0.5f);
            _spriteBatch.Draw(_crosshairTexture, center, null, Color.White, 0f, crosshairOrigin, 1f, SpriteEffects.None, 0f);
        }

        if (_font != null)
        {
            _spriteBatch.DrawString(_font, $"Score: {_score}", new Vector2(20f, 20f), Color.White);
            _spriteBatch.DrawString(_font, "Controls: WASD to move, mouse to look, Left click / Space to shoot, Shift to sprint", new Vector2(20f, viewport.Height - 40f), Color.White);
        }
    }

    private Texture2D CreateCrosshairTexture(GraphicsDevice device, int size, int thickness, int gap)
    {
        var texture = new Texture2D(device, size, size);
        var data = new Color[size * size];
        var half = size / 2;
        var halfThickness = thickness / 2;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var index = y * size + x;
                var color = Color.Transparent;

                var horizontal = Math.Abs(y - half) <= halfThickness && Math.Abs(x - half) > gap;
                var vertical = Math.Abs(x - half) <= halfThickness && Math.Abs(y - half) > gap;

                if (horizontal || vertical)
                {
                    color = Color.White;
                }

                data[index] = color;
            }
        }

        texture.SetData(data);
        return texture;
    }

    private void CenterMouse()
    {
        if (!IsActive)
            return;

        var viewport = GraphicsDevice.Viewport;
        var center = new Point(viewport.Width / 2, viewport.Height / 2);
        Mouse.SetPosition(center.X, center.Y);
    }
}
