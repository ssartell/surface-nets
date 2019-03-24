using System;
using System.Collections;
using System.Collections.Generic;
using Assets;
using UnityEngine;

public class SurfaceNets : MonoBehaviour
{
    private float _isoLevel = 0.0f;
    private int _size = 32;

    private List<Edge> _edges;
    private List<int> _intersections;
    private Corner[] _corners;
    private Cube[,,] _cubes;
    private List<Vector3> _vertices;
    private List<int> _triangles;
    private Voxel[,,] _voxels;
    private Func<Vector3, float> _sdf;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("starting surface nets");
        _corners = Corners();
        _edges = EdgesTable();
        _intersections = IntersectionsTable(_edges);

        // wormy plane
        //_sdf = Sdf.Intersection(
        //        Sdf.Union(
        //            Sdf.Plane(Vector3.up)
        //                .Translate(new Vector3(0, 16, 0)),
        //            Sdf.Sphere()
        //                .Scale(8)
        //                .Translate(Vector3.one * 16)),
        //        Sdf.Perlin(this._size / 5f))
        //    .Transform(transform)
        //    .ToFunc();

        // wormy sphere
        _sdf = Sdf.Intersection(
            Sdf.Sphere()
                .Scale(40),
            Sdf.Perlin(5))
            .Transform(transform)
            .ToFunc();

        // heart
        //_sdf = Sdf.Sphere()
        //    .Scale(15)
        //    .Transform(p => new Vector3(
        //        p.x, 
        //        4 + 1.2f * p.y - Mathf.Abs(p.x) * Mathf.Sqrt((20 - Mathf.Abs(p.x)) / 20f), 
        //        p.z * (2 - p.y / 15f)))
        //    .Translate(Vector3.one * 20)
        //    .Transform(transform)
        //    .ToFunc();


        _voxels = GenerateVoxels(_sdf);

        GenerateMesh();

        if (_vertices.Count > 65536)
        {
            Debug.LogWarning("Exceeded max vertex count of 65536 (2^16).");
        }

        var mesh = new Mesh
        {
            vertices = this._vertices.ToArray(),
            triangles = this._triangles.ToArray()
        };

        mesh.RecalculateNormals();

        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void GenerateMesh()
    {
        _vertices = new List<Vector3>();
        _triangles = new List<int>();

        _cubes = GenerateCubes(_isoLevel, _voxels);
        var size = _cubes.GetLength(0);

        for (var x = 1; x < size; x++)
        {
            for (var y = 1; y < size; y++)
            {
                for (var z = 1; z < size; z++)
                {
                    var cube = _cubes[x, y, z];
                    if (!cube.IsOnSurface) continue;

                    for (var i = 0; i < 3; i++)
                    {
                        if ((cube.EdgeMask & (1 << i)) == 0) continue;
                        var v0 = 0;
                        var v1 = 0;
                        var v2 = 0;
                        var v3 = 0;
                        if (i == 0)
                        {
                            v0 = cube.VertexIndex;
                            v1 = _cubes[x, y - 1, z].VertexIndex;
                            v2 = _cubes[x, y - 1, z - 1].VertexIndex;
                            v3 = _cubes[x, y, z - 1].VertexIndex;
                        }
                        else if (i == 1)
                        {
                            v0 = cube.VertexIndex;
                            v1 = _cubes[x - 1, y, z].VertexIndex;
                            v2 = _cubes[x - 1, y - 1, z].VertexIndex;
                            v3 = _cubes[x, y - 1, z].VertexIndex;
                        }
                        else if (i == 2)
                        {
                            v0 = cube.VertexIndex;
                            v1 = _cubes[x, y, z - 1].VertexIndex;
                            v2 = _cubes[x - 1, y, z - 1].VertexIndex;
                            v3 = _cubes[x - 1, y, z].VertexIndex;
                        }

                        if (cube.Value < _isoLevel)
                        {
                            _triangles.AddRange(new[] { v0, v1, v2 });
                            _triangles.AddRange(new[] { v0, v2, v3 });
                        }
                        else
                        {
                            _triangles.AddRange(new[] { v0, v2, v1 });
                            _triangles.AddRange(new[] { v0, v3, v2 });
                        }
                    }
                }

            }

        }
    }

    private Cube[,,] GenerateCubes(float isoLevel, Voxel[,,] voxels)
    {
        var vertexIndex = 0;
        var size = voxels.GetLength(0) - 1;
        var cubes = new Cube[size, size, size];
        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                for (var z = 0; z < size; z++)
                {
                    var cornerVoxels = new Voxel[8];
                    var cornerMask = 0;
                    for (var i = 0; i < 8; i++)
                    {
                        var corner = _corners[i];
                        var voxel = voxels[x + corner.x, y + corner.y, z + corner.z];
                        cornerVoxels[i] = voxel;
                        cornerMask |= voxel.Value >= isoLevel ? 1 << i : 0;
                    }

                    if (cornerMask == 0 || cornerMask == 0xff) continue; // all corners are inside or outside the surface

                    var edgeMask = _intersections[cornerMask];

                    var vertex = Vector3.zero;
                    var crossings = 0;
                    for (var i = 0; i < _edges.Count; i++)
                    {
                        if ((edgeMask & (1 << i)) == 0) continue;
                        crossings++;

                        var edge = _edges[i];
                        var x0 = cornerVoxels[edge.i];
                        var x1 = cornerVoxels[edge.j];
                        var y0 = x0.Value;
                        var y1 = x1.Value;

                        var t = (isoLevel - y0) / (y1 - y0);
                        vertex += Vector3.Lerp(x0.Position, x1.Position, t);
                    }

                    vertex /= crossings;

                    // minecraft-y
                    //vertex = (cornerVoxels[0].Position + cornerVoxels[7].Position) / 2;

                    var cube = new Cube()
                    {
                        CornerMask = cornerMask,
                        IsOnSurface = cornerMask != 0 && cornerMask != 0xff,
                        EdgeMask = _intersections[cornerMask],
                        VertexPosition = vertex,
                        VertexIndex = vertexIndex,
                        Value = voxels[x, y, z].Value
                    };

                    cubes[x, y, z] = cube;
                    _vertices.Add(vertex);
                    vertexIndex++;
                }
            }
        }

        return cubes;
    }

    private Voxel[,,] GenerateVoxels(Func<Vector3, float> sdf)
    {

        var voxels = new Voxel[this._size, this._size, this._size];
        for (var x = 0; x < this._size; x++)
        {
            for (var y = 0; y < this._size; y++)
            {
                for (var z = 0; z < this._size; z++)
                {
                    var p = new Vector3(x, y, z);
                    voxels[x, y, z] = new Voxel()
                    {
                        Position = p,
                        Value = sdf(p)
                    };
                }
            }
        }

        return voxels;
    }

    List<Edge> EdgesTable()
    {
        var edges = new List<Edge>();
        var k = 0;
        for (var i = 0; i < 8; ++i) // every corner
        {
            for (var j = 1; j <= 4; j <<= 1)    // adjacent corners
            {
                var p = i ^ j;
                if (i <= p) // not already added
                {
                    edges.Add(new Edge()
                    {
                        i = i,
                        j = p,
                    });
                }
            }
        }

        return edges;
    }

    List<int> IntersectionsTable(List<Edge> edges)
    {
        var intersections = new List<int>();
        for (int i = 0; i < 256; ++i)   // every combination of corners in or out of isosurface
        {
            int edgeMask = 0;
            for (int j = 0; j < edges.Count; j++)    // every edge
            {
                var edge = edges[j];
                var a = Convert.ToBoolean(i & (1 << edge.i));   // is first corner in?
                var b = Convert.ToBoolean(i & (1 << edge.j));   // is second corner in?
                edgeMask |= a != b ? (1 << j) : 0;   // if they are on different sides, add edge to mask
            }
            intersections.Add(edgeMask);
        }

        return intersections;
    }

    private static Corner[] Corners()
    {
        return new[] {
            new Corner(0,0,0),	// 0
            new Corner(1,0,0), 	// 1
            new Corner(0,0,1), 	// 2
            new Corner(1,0,1), 	// 3
            new Corner(0,1,0), 	// 4
            new Corner(1,1,0), 	// 5
            new Corner(0,1,1), 	// 6
            new Corner(1,1,1)  	// 7
        };
    }

    void OnDrawGizmos()
    {
        //Gizmos.color = Color.white;
        //Gizmos.DrawWireMesh(this.GetComponent<MeshFilter>().mesh, transform.localPosition);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(Vector3.one * (this._size - 1) / 2f + transform.localPosition, transform.TransformVector(Vector3.one * (this._size - 1)));

        //for (var x = 0; x < _size - 1; x++)
        //{
        //    for (var y = 0; y < _size - 1; y++)
        //    {
        //        for (var z = 0; z < _size - 1; z++)
        //        {
        //            if (_cubes[x, y, z].IsOnSurface)
        //            {
        //                Gizmos.DrawWireCube(new Vector3(x + .5f, y + .5f, z + .5f) + transform.localPosition, Vector3.one);
        //            }
        //        }
        //    }
        //}
    }
}

public struct Cube
{
    public bool IsOnSurface;
    public int EdgeMask;
    public int CornerMask;
    public Vector3 VertexPosition;
    public int VertexIndex;
    public float Value;
}

public struct Voxel
{
    public float Value;
    public Vector3 Position;
}

public struct Edge
{
    public int i;
    public int j;
}

public struct Corner
{
    public int x;
    public int y;
    public int z;

    public Corner(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}