using System;

using System.Collections.Generic;
using System.Text;

namespace OpenGLES
{
    public abstract class Animation<T>
    {
        public T Current
        {
            get;
            set;
        }

        public abstract T Animate(float timeStep);
    }

    public interface IAnimatable<T, P>
    {
        T Add(T other);
        T Subtract(T other);
        T Scale(P scalar);
        P Length
        {
            get;
        }
        P LengthSquare
        {
            get;
        }
    }

    public class LinearAnimation<T> : Animation<T> where T : IAnimatable<T, float>
    {
        public T Target
        {
            get;
            set;
        }

        public float TimeLeft
        {
            get;
            set;
        }

        public override T Animate(float timeStep)
        {
            T vector = Target.Subtract(Current);

            if (timeStep < 0)
                timeStep = 0;
            if (timeStep > TimeLeft)
                timeStep = TimeLeft;

            float relativeTimeElapsed = timeStep / TimeLeft;
            TimeLeft -= timeStep;
            vector = vector.Scale(relativeTimeElapsed);
            Current = Current.Add(vector);
            return Current;
        }
    }

    public class Vector3fAnimation : LinearAnimation<Vector3f>
    {
    }

    public class DecelerationAnimation<T> : Animation<T> where T : IAnimatable<T, float>, new()
    {
        float myDecelerationRate = 1;
        public float DecelerationRate
        {
            get
            {
                return myDecelerationRate;
            }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Deceleration rate must be greater than 0");
                myDecelerationRate = value;
            }
        }

        public override T Animate(float timeStep)
        {
            float lsq = Current.LengthSquare;
            if (lsq == 0)
                return Current;
            float l = (float)Math.Sqrt(lsq);
            T deceleration = Current.Scale(1f / l).Scale(myDecelerationRate);

            if (deceleration.LengthSquare > Current.LengthSquare)
                return Current = new T();

            Current = Current.Subtract(deceleration);
            return Current;
        }
    }
}
