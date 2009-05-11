using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGLES
{
    public struct Matrix44f
    {
        public Vector4f V0;
        public Vector4f V1;
        public Vector4f V2;
        public Vector4f V3;

        public static Matrix44f CreateYBillboardMatrix(Matrix44f matrix)
        {
            double theta = -Math.Atan2(matrix.V2.X, matrix.V2.Z);
            float ct = (float)Math.Cos(theta);
            float st = (float)Math.Sin(theta);
            float x = 0;
            float y = 1;
            float z = 0;

            Matrix44f ret = new Matrix44f();
            ret.V0 = new Vector4f(1, 0, 0, 0);
            ret.V1 = new Vector4f(0, 1, 0, 0);
            ret.V2 = new Vector4f(0, 0, 1, 0);
            ret.V3 = new Vector4f(0, 0, 0, 1);

            ret.V0.X = x * x + ct * (1 - x * x) + st * 0;
            ret.V1.X = x * y + ct * (0 - x * y) + st * -z;
            ret.V2.X = x * z + ct * (0 - x * z) + st * y;

            ret.V0.Y = y * x + ct * (0 - y * x) + st * z;
            ret.V1.Y = y * y + ct * (1 - y * y) + st * 0;
            ret.V2.Y = y * z + ct * (0 - y * z) + st * -x;

            ret.V0.Z = z * x + ct * (0 - z * x) + st * -y;
            ret.V1.Z = z * y + ct * (0 - z * y) + st * x;
            ret.V2.Z = z * z + ct * (1 - z * z) + st * 0;

            return ret;
        }

        public static Matrix44f CreateBillboardMatrix(Vector3f pos, Vector3f camPos, Vector3f camUp)
        {
            Vector3f look = camPos - pos;
            look = look.Normalize();

            Vector3f right = camUp.CrossProduct(look);
            Vector3f up = look.CrossProduct(right);

            Matrix44f ret = new Matrix44f();
            ret.V0 = right;
            ret.V1 = up;
            ret.V2 = look;
            ret.V3 = new Vector4f(pos, 1);

            return ret;
        }

        public Matrix44f Transpose()
        {
            Matrix44f ret = new Matrix44f();
            ret.V0 = new Vector4f(V0.X, V1.X, V2.X, V3.X);
            ret.V1 = new Vector4f(V0.Y, V1.Y, V2.Y, V3.Y);
            ret.V2 = new Vector4f(V0.Z, V1.Z, V2.Z, V3.Z);
            ret.V3 = new Vector4f(V0.W, V1.W, V2.W, V3.W);
            return ret;
        }

        public static Vector3f operator *(Matrix44f matrix, Vector3f vector)
        {
            return new Vector3f(matrix.V0.DotProduct(vector) + matrix.V0.W, matrix.V1.DotProduct(vector) + matrix.V1.W, matrix.V2.DotProduct(vector) + matrix.V2.W);
        }
    }
}
