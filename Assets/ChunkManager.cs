using System;
using System.Collections;
using System.Collections.Generic;
using Assets;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public int ChunksX = 1;
    public int ChunksY = 1;
    public int ChunksZ = 1;

    public int ChunkSize = 32;
    public int Lod = 2;
    public float IsoLevel = 0f;

    public Material Material;

    private Chunk[,,] _chunks;
    private ChunkRenderer[,,] _chunkRenderers;

    // Start is called before the first frame update
    void Start()
    {
        var sdf = GetSdf();

        var size = this.ChunkSize;  // chunks should overlap by one voxel
        var offset = new Vector3(this.ChunksX, this.ChunksY, this.ChunksZ) * size / 2f;
        _chunks = new Chunk[this.ChunksX, this.ChunksY, this.ChunksZ];
        _chunkRenderers = new ChunkRenderer[this.ChunksX, this.ChunksY, this.ChunksZ];
        var edges = EdgesTable();
        var intersections = IntersectionsTable(edges);

        for (var x = 0; x < this.ChunksX; x++)
        {
            for (var y = 0; y < this.ChunksY; y++)
            {
                for (var z = 0; z < this.ChunksZ; z++)
                {
                    var index = new Vector3(x, y, z);
                    var position = index * size - offset;
                    var renderedChunk = new GameObject($"Chunk[{x},{y},{z}]");

                    var chunk = GenerateChunk(sdf, position, index, ChunkSize, Lod);
                    _chunks[x, y, z] = chunk;

                    var renderer = renderedChunk.AddComponent<MeshRenderer>();
                    renderer.material = this.Material;

                    var chunkRenderer = renderedChunk.AddComponent<ChunkRenderer>();
                    chunkRenderer.Chunk = chunk;
                    chunkRenderer.Chunks = _chunks;
                    chunkRenderer.Edges = edges;
                    chunkRenderer.Intersections = intersections;
                    chunkRenderer.ChunkRenderers = _chunkRenderers;
                    chunkRenderer.IsoLevel = IsoLevel;

                    renderedChunk.transform.parent = transform;
                    renderedChunk.transform.position = position;

                    _chunkRenderers[x, y, z] = chunkRenderer;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private Func<Vector3, float> GetSdf()
    {
        // wormy plane
        //return Sdf.Intersection(
        //        Sdf.Union(
        //            Sdf.Plane(Vector3.up)
        //                .Translate(new Vector3(0, 16, 0)),
        //            Sdf.Sphere()
        //                .Scale(8)
        //                .Translate(Vector3.one * 16)),
        //        Sdf.Perlin(this.Size / 5f))
        //    .Transform(transform)
        //    .ToFunc();

        // wormy sphere
        //return Sdf.Intersection(
        //    Sdf.Sphere()
        //        .Scale(40),
        //    Sdf.Perlin(5))
        //    .Transform(transform)
        //    .ToFunc();

        return Sdf.Sphere()
            .Scale(40)
            .Transform(transform)
            .ToFunc();

        // heart
        //return Sdf.Sphere()
        //    .Scale(15)
        //    .Transform(p => new Vector3(
        //        p.x, 
        //        4 + 1.2f * p.y - Mathf.Abs(p.x) * Mathf.Sqrt((20 - Mathf.Abs(p.x)) / 20f), 
        //        p.z * (2 - p.y / 15f)))
        //    .Translate(Vector3.one * 20)
        //    .Transform(transform)
        //    .ToFunc();
    }

    private Chunk GenerateChunk(Func<Vector3, float> sdf, Vector3 position, Vector3 index, int chunkSize, int lod)
    {
        var size = chunkSize / lod;
        var voxels = new Voxel[size + 1, size + 1, size + 1];
        for (var x = 0; x <= size; x++)
        {
            for (var y = 0; y <= size; y++)
            {
                for (var z = 0; z <= size; z++)
                {
                    var p = new Vector3(x * lod, y * lod, z * lod);
                    voxels[x, y, z] = new Voxel()
                    {
                        Position = p,
                        Value = sdf(position + p)
                    };
                }
            }
        }

        return new Chunk()
        {
            Position = position,
            Index = index,
            Size = chunkSize,
            Lod = lod,
            Voxels = voxels
        }; ;
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
}

public struct Chunk
{
    public int Size;
    public int Lod;
    public Vector3 Position;
    public Vector3 Index;
    public Voxel[,,] Voxels;
}

public struct Voxel
{
    public float Value;
    public Vector3 Position;
}