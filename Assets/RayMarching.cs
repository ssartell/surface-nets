using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayMarching : MonoBehaviour
{
    public Camera Camera;

    private Mesh _mesh;
    private List<Vector3> _vertices;
    private List<int> _triangles;

    private const int xSteps = 200;
    private const int ySteps = 200;
    private const float dx = 1.0f / xSteps;
    private const float dy = 1.0f / ySteps;
    private const int maxMarchingSteps = 10;
    private const float epsilon = 0.001f;
    private const float maxDist = 10f;


    // Start is called before the first frame update
    void Start()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
    }

    // Update is called once per frame
    void Update()
    {
        _vertices = new List<Vector3>();
        _triangles = new List<int>();

        for (var x = 0; x <= xSteps; x++)
        {
            var px = x * dx;
            for (var y = 0; y <= ySteps; y++)
            {
                var py = y * dy;
                var ray = Camera.ViewportPointToRay(new Vector3(px, py, 0));
                var dist = MarchRay(ray, SphereSdf);
                _vertices.Add(ray.origin + ray.direction * dist);

                if (x >= xSteps || y >= ySteps) continue;

                _triangles.AddRange(new [] { y + xSteps * x, (y + 1) + xSteps * x, (y + 1) + xSteps * (x + 1) });
                _triangles.AddRange(new[] { y + xSteps * x, (y + 1) + xSteps * (x + 1), y + xSteps * (x + 1) });
            }
        }

        _mesh.Clear();
        _mesh.vertices = _vertices.ToArray();
        _mesh.triangles = _triangles.ToArray();
        _mesh.RecalculateNormals();
    }

    float MarchRay(Ray ray, Func<Vector3, float> sdf)
    {
        float depth = 0;
        for (var i = 0; i < maxMarchingSteps; i++)
        {
            var dist = sdf(ray.origin + ray.direction * depth);
            if (dist < epsilon) return depth;

            depth += dist;

            if (depth >= maxDist) return maxDist;
        }

        return maxDist;
    }

    float SphereSdf(Vector3 p)
    {
        return (float)Math.Sqrt((double)(p.x * p.x + p.y * p.y + p.z * p.z)) - 1.0f;
    }

    Func<Vector3, float> CreateTorusSdf(Vector2 t)
    {
        return (Vector3 p) =>
        {
            Vector2 q = new Vector2(new Vector2(p.x, p.z).magnitude - t.x, p.y);
            return q.magnitude - t.y;
        };
    }


}
