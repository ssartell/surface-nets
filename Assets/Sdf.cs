using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets
{
    public class Sdf
    {
        private Func<Vector3, float> _sdf;

        private Sdf(Func<Vector3, float> sdf)
        {
            _sdf = sdf;
        }

        public static Sdf Sphere()
        {
            return new Sdf(p => (float) Math.Sqrt(p.x * p.x + p.y * p.y + p.z * p.z) - 1.0f);
        }

        public static Sdf Torus(Vector2 radii)
        {
            return new Sdf(p =>
            {
                var q = new Vector2((new Vector2(p.x, p.z)).magnitude - radii.x, p.y);
                return q.magnitude - radii.y;
            });
        }

        public static Sdf Box(Vector3 b)
        {
            return new Sdf(p =>
            {
                var d = new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z)) - b;
                return (new Vector3(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f), Mathf.Max(d.z, 0f))).magnitude
                       + Mathf.Min(Mathf.Max(d.x, Mathf.Max(d.y, d.z)), 0.0f);
            });
        }

        public static Sdf Plane(Vector3 normal)
        {
            return new Sdf(p => Vector3.Dot(p, normal));
        }

        public static Sdf Perlin(float scale)
        {
            return new Sdf(p =>
            {
                p = p / scale;
                float ab = Mathf.PerlinNoise(p.x, p.y);
                float bc = Mathf.PerlinNoise(p.y, p.z);
                float ac = Mathf.PerlinNoise(p.x, p.z);

                float ba = Mathf.PerlinNoise(p.y, p.x);
                float cb = Mathf.PerlinNoise(p.z, p.y);
                float ca = Mathf.PerlinNoise(p.z, p.x);

                float abc = ab + bc + ac + ba + cb + ca;
                return abc / 6f - .5f;
            });
        }

        public Sdf Scale(float s)
        {
            return new Sdf(p => _sdf(p / s) * s);
        }

        public Sdf Translate(Vector3 q)
        {
            return new Sdf(p => _sdf(p - q));
        }

        public Sdf Rotate(Vector3 euler)
        {
            var rotation = Quaternion.Euler(euler);
            return new Sdf(p => _sdf(rotation * p));
        }

        public Sdf Transform(Transform transform)
        {
            return new Sdf(p => _sdf(transform.TransformVector(p) + transform.localPosition));
        }

        public Sdf Negate()
        {
            return new Sdf(p => - _sdf(p));
        }

        public static Sdf Union(Sdf sdf1, Sdf sdf2)
        {
            return new Sdf(p => Mathf.Min(sdf1.ToFunc()(p), sdf2.ToFunc()(p)));
        }

        public static Sdf Subtract(Sdf sdf1, Sdf sdf2)
        {
            return Intersection(sdf1.Negate(), sdf2);
        }

        public static Sdf Intersection(Sdf sdf1, Sdf sdf2)
        {
            return new Sdf(p => Mathf.Max(sdf1.ToFunc()(p), sdf2.ToFunc()(p)));
        }

        public Func<Vector3, float> ToFunc()
        {
            return _sdf;
        }
    }
}
