using Microsoft.Xna.Framework;

namespace Monogame;

internal sealed class FirstPersonCamera
{
    private readonly float _eyeHeight;
    private Vector3 _position;
    private float _aspectRatio;

    public FirstPersonCamera(float aspectRatio, float eyeHeight = 1.6f)
    {
        _aspectRatio = aspectRatio;
        _eyeHeight = eyeHeight;
        _position = new Vector3(0f, _eyeHeight, 0f);
        Yaw = 0f;
        Pitch = 0f;
        UpdateProjection(aspectRatio);
        UpdateView();
    }

    public Vector3 Position => _position;
    public float Pitch { get; private set; }
    public float Yaw { get; private set; }
    public Matrix View { get; private set; } = Matrix.Identity;
    public Matrix Projection { get; private set; } = Matrix.Identity;
    public Vector3 Forward
    {
        get
        {
            var forward = new Vector3(
                (float)(System.Math.Cos(Pitch) * System.Math.Sin(Yaw)),
                (float)System.Math.Sin(Pitch),
                (float)(System.Math.Cos(Pitch) * System.Math.Cos(Yaw)));
            if (forward.LengthSquared() < float.Epsilon)
            {
                return Vector3.Forward;
            }

            forward.Normalize();
            return forward;
        }
    }

    public Vector3 ForwardOnPlane
    {
        get
        {
            var forward = Forward;
            forward.Y = 0f;
            if (forward.LengthSquared() < float.Epsilon)
            {
                return Vector3.Forward;
            }

            forward.Normalize();
            return forward;
        }
    }

    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.Up));
    public Vector3 RightOnPlane
    {
        get
        {
            var forward = ForwardOnPlane;
            var right = Vector3.Cross(forward, Vector3.Up);
            if (right.LengthSquared() < float.Epsilon)
            {
                return Vector3.Right;
            }

            right.Normalize();
            return right;
        }
    }

    public void UpdateProjection(float aspectRatio)
    {
        _aspectRatio = aspectRatio;
        Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(75f), _aspectRatio, 0.1f, 150f);
    }

    public void SetPosition(Vector3 position)
    {
        _position = new Vector3(position.X, _eyeHeight, position.Z);
        UpdateView();
    }

    public void Move(Vector3 translation)
    {
        _position += translation;
        _position.Y = _eyeHeight;
        UpdateView();
    }

    public void Rotate(float deltaYaw, float deltaPitch)
    {
        Yaw = MathHelper.WrapAngle(Yaw - deltaYaw);
        Pitch = MathHelper.Clamp(Pitch - deltaPitch, MathHelper.ToRadians(-89f), MathHelper.ToRadians(89f));
        UpdateView();
    }

    public void UpdateView()
    {
        View = Matrix.CreateLookAt(_position, _position + Forward, Vector3.Up);
    }
}
