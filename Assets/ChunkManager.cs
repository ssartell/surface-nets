using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public int ChunksX = 3;
    public int ChunksY = 3;

    public Material Material;

    // Start is called before the first frame update
    void Start()
    {
        for (var x = 0; x < this.ChunksX; x++)
        {
            for (var z = 0; z < this.ChunksY; z++)
            {
                var chunk = new GameObject();
                
                var renderer = chunk.AddComponent<MeshRenderer>();
                renderer.material = this.Material;

                var surfaceNets = chunk.AddComponent<SurfaceNets>();

                chunk.transform.parent = transform;
                chunk.transform.position = new Vector3(x, 0, z) * 30;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
