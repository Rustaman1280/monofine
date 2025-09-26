using Microsoft.Xna.Framework;

namespace Monogame;

internal sealed class Enemy
{
    private static readonly Vector3 HalfUnit = new(0.5f);

    public Enemy(float size)
    {
        Size = size;
    }

    public Vector3 Position { get; private set; } = Vector3.Zero;
    public Color Color { get; private set; } = Color.White;
    public float Size { get; }
    public bool IsAlive { get; private set; }

    public BoundingBox Bounds
    {
        get
        {
            var half = HalfUnit * Size;
            return new BoundingBox(Position - half, Position + half);
        }
    }

    public void Respawn(Vector3 position, Color color)
    {
        Position = position;
        Color = color;
        IsAlive = true;
    }

    public void Kill()
    {
        IsAlive = false;
    }
}
