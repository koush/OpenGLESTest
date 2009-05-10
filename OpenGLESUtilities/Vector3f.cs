using System;

namespace OpenGLES
{
    public struct Vector3f : IAnimatable<Vector3f, float>
    {
        public Vector3f(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X;
        public float Y;
        public float Z;

        public float Length
        {
            get
            {
                return (float)Math.Sqrt(LengthSquare);
            }
            set
            {
            }
        }


        public float LengthSquare
        {
            get
            {
                return X * X + Y * Y + Z * Z;
            }
        }

        public Vector3f Normalize()
        {
            return Scale(1 / Length);
        }

        public Vector3f Scale(float scale)
        {
            Vector3f ret = this;
            ret.X *= scale;
            ret.Y *= scale;
            ret.Z *= scale;
            return ret;
        }

        public static Vector3f operator +(Vector3f one, Vector3f two)
        {
            one.X += two.X;
            one.Y += two.Y;
            one.Z += two.Z;
            return one;
        }

        public static Vector3f operator -(Vector3f one, Vector3f two)
        {
            one.X -= two.X;
            one.Y -= two.Y;
            one.Z -= two.Z;
            return one;
        }

        public float DotProduct(Vector3f other)
        {
            return X * other.X + Y * other.Y + Z * other.Z;
        }

        public Vector3f CrossProduct(Vector3f other)
        {
            Vector3f ret = new Vector3f();
            ret.X = Y * other.Z - other.Y * Z;
            ret.Y = Z * other.X - other.Z * X;
            ret.Z = X * other.Y - other.X * Y;
            return ret;
        }

        public static readonly Vector3f Zero = new Vector3f(0, 0, 0);

        #region IOperatable<Vector3f,float> Members

        public Vector3f Subtract(Vector3f other)
        {
            return this - other;
        }

        public Vector3f Add(Vector3f other)
        {
            return this + other;
        }

        #endregion
    }
}