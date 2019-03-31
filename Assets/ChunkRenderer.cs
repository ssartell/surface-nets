using System;
using System.Collections.Generic;
using UnityEngine;

public class ChunkRenderer : MonoBehaviour
{
    public float IsoLevel = 0f;
    public Chunk Chunk;
    public Chunk[,,] Chunks;
    public ChunkRenderer[,,] ChunkRenderers;
    public Cube[,,] Cubes;
    public List<Edge> Edges;
    public List<int> Intersections;

    private readonly Corner[] _corners = {
        new Corner(0,0,0),	// 0
        new Corner(1,0,0), 	// 1
        new Corner(0,0,1), 	// 2
        new Corner(1,0,1), 	// 3
        new Corner(0,1,0), 	// 4
        new Corner(1,1,0), 	// 5
        new Corner(0,1,1), 	// 6
        new Corner(1,1,1)  	// 7
    };

    private List<Vector3> _vertices;
    private List<int> _triangles;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("starting surface nets");
        _vertices = new List<Vector3>();
        _triangles = new List<int>();

        GenerateCubes();
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

    public Cube GetCube(int x, int y, int z)
    {
        return Cubes[x / Chunk.Lod, x / Chunk.Lod, x / Chunk.Lod];
    }

    private void GenerateMesh()
    {
        var size = Cubes.GetLength(0);

        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                for (var z = 0; z < size; z++)
                {
                    var cube = Cubes[x, y, z];
                    if (!cube.IsOnSurface) continue;

                    for (var i = 0; i < 3; i++)
                    {
                        if ((cube.EdgeMask & (1 << i)) == 0) continue;
                        var v0 = 0;
                        var v1 = 0;
                        var v2 = 0;
                        var v3 = 0;

                        if (x * y * z != 0)
                        {
                            if (i == 0)
                            {
                                v0 = cube.VertexIndex;
                                v1 = Cubes[x, y - 1, z].VertexIndex;
                                v2 = Cubes[x, y - 1, z - 1].VertexIndex;
                                v3 = Cubes[x, y, z - 1].VertexIndex;
                            }
                            else if (i == 1)
                            {
                                v0 = cube.VertexIndex;
                                v1 = Cubes[x - 1, y, z].VertexIndex;
                                v2 = Cubes[x - 1, y - 1, z].VertexIndex;
                                v3 = Cubes[x, y - 1, z].VertexIndex;
                            }
                            else if (i == 2)
                            {
                                v0 = cube.VertexIndex;
                                v1 = Cubes[x, y, z - 1].VertexIndex;
                                v2 = Cubes[x - 1, y, z - 1].VertexIndex;
                                v3 = Cubes[x - 1, y, z].VertexIndex;
                            }

                            if (cube.Value < IsoLevel)
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
                        else
                        {

                        }
                    }
                }

            }

        }
    }

    private void GenerateCubes()
    {
        var vertexIndex = 0;
        var size = Chunk.Voxels.GetLength(0) - 1;
        Cubes = new Cube[size, size, size];
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
                        var voxel = Chunk.Voxels[x + corner.x, y + corner.y, z + corner.z];
                        cornerVoxels[i] = voxel;
                        cornerMask |= voxel.Value >= IsoLevel ? 1 << i : 0;
                    }

                    if (cornerMask == 0 || cornerMask == 0xff) continue; // all corners are inside or outside the surface

                    var edgeMask = Intersections[cornerMask];

                    var vertex = Vector3.zero;
                    var crossings = 0;
                    for (var i = 0; i < Edges.Count; i++)
                    {
                        if ((edgeMask & (1 << i)) == 0) continue;
                        crossings++;

                        var edge = Edges[i];
                        var x0 = cornerVoxels[edge.i];
                        var x1 = cornerVoxels[edge.j];
                        var y0 = x0.Value;
                        var y1 = x1.Value;

                        var t = (IsoLevel - y0) / (y1 - y0);
                        vertex += Vector3.Lerp(x0.Position, x1.Position, t);
                    }

                    vertex /= crossings;

                    // minecraft-y
                    //vertex = (cornerVoxels[0].Position + cornerVoxels[7].Position) / 2;

                    var cube = new Cube()
                    {
                        CornerMask = cornerMask,
                        IsOnSurface = cornerMask != 0 && cornerMask != 0xff,
                        EdgeMask = Intersections[cornerMask],
                        VertexPosition = vertex,
                        VertexIndex = vertexIndex,
                        Value = Chunk.Voxels[x, y, z].Value
                    };

                    Cubes[x, y, z] = cube;
                    _vertices.Add(vertex);
                    vertexIndex++;
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        //Gizmos.color = Color.white;
        //Gizmos.DrawWireMesh(this.GetComponent<MeshFilter>().mesh, transform.localPosition);
        var size = Cubes.GetLength(0);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(Vector3.one * Chunk.Size / 2f + transform.localPosition, transform.TransformVector(Vector3.one * Chunk.Size));

        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                for (var z = 0; z < size; z++)
                {
                    if (Cubes[x, y, z].IsOnSurface)
                    {
                        Gizmos.DrawWireCube(new Vector3(x + .5f, y + .5f, z + .5f) * Chunk.Lod + transform.localPosition, Vector3.one * Chunk.Lod);
                    }
                }
            }
        }
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