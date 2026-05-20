namespace SEUtilityTools.API.Struct
{
    public struct Vector3
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public readonly float Magnitude => MathF.Sqrt(X * X + Y * Y + Z * Z);

        public Vector3 Normalize()
        {
            float mag = Magnitude;
            return mag == 0 ? new Vector3(0, 0, 0) : new Vector3(X / mag, Y / mag, Z / mag);
        }

        public static float Distance(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator *(Vector3 v, float scalar) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);
        public static Vector3 operator /(Vector3 v, float scalar) => new(v.X / scalar, v.Y / scalar, v.Z / scalar);

        public override readonly string ToString() => $"({X}, {Y}, {Z})";
    }
}