using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshScript : MonoBehaviour
{
    private Mesh _mesh;
    private List<Vector3> _vertices = new List<Vector3>();
    private List<int> _triangles = new List<int>();

    // Start is called before the first frame update
    void Start()
    {
        _mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh = _mesh;       
        CreateMesh();
    }

    private void CreateMesh()
    {
        var d = 100;
        var verts = d * 2;
        var radius = 10.0;
        var subRadius = 1.0;
        var angleDelta = 2.0 * Math.PI / d;

        for (var i = 0; i <=  2 * d; i += 2)
        {
            var angle = i * angleDelta;
            var subAngle = angle * .5;
            var x1 = (radius + subRadius * Math.Cos(subAngle)) * Math.Cos(angle);
            var y1 = subRadius * Math.Sin(subAngle);
            var z1 = (radius + subRadius * Math.Cos(subAngle)) * Math.Sin(angle);

            _vertices.Add(new Vector3((float)x1, (float)y1, (float)z1));

            subAngle += Math.PI;
            var x2 = (radius + subRadius * Math.Cos(subAngle)) * Math.Cos(angle);
            var y2 = subRadius * Math.Sin(subAngle);
            var z2 = (radius + subRadius * Math.Cos(subAngle)) * Math.Sin(angle);

            _vertices.Add(new Vector3((float)x2, (float)y2, (float)z2));

            _triangles.AddRange(new [] { i % verts, (i + 1) % verts, (i + 3) % verts });
            _triangles.AddRange(new [] { i % verts, (i + 3) % verts, (i + 2) % verts });

            _triangles.AddRange(new[] { i % verts, (i + 3) % verts, (i + 1) % verts });
            _triangles.AddRange(new[] { i % verts, (i + 2) % verts, (i + 3) % verts });
        }

        _mesh.Clear();
        _mesh.vertices = _vertices.ToArray();
        _mesh.triangles = _triangles.ToArray();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
