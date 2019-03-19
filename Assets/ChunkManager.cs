using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public int ChunksX = 3;
    public int ChunksY = 1;
    public int ChunksZ = 3;

    public int ChunkSize = 32;

    public Material Material;

    // Start is called before the first frame update
    void Start()
    {
        var size = (this.ChunkSize - 2);
        var offset = new Vector3(this.ChunksX, this.ChunksY, this.ChunksZ) * size / 2f;
        for (var x = 0; x < this.ChunksX; x++)
        {
            for (var y = 0; y < this.ChunksY; y++)
            {
                for (var z = 0; z < this.ChunksZ; z++)
                {
                    var chunk = new GameObject();

                    var renderer = chunk.AddComponent<MeshRenderer>();
                    renderer.material = this.Material;

                    var surfaceNets = chunk.AddComponent<SurfaceNets>();

                    chunk.transform.parent = transform;
                    chunk.transform.position = new Vector3(x, y, z) * size - offset;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
