using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System;

[Serializable]
public struct Variables
{
    public Vector2 Dimension;
    [Range(275,3000)] public int Resolution;
    public float UVScale;
    public float Seed, Height;
    public float falloffStart, falloffEnd;
}

[Serializable]
public struct Waves
{
    public Vector2 speed;
    public Vector2 scale;
    public float height;
    public bool alternate;
}


public class MeshGeneration : MonoBehaviour
{
    MeshFilter meshFilter;

    Mesh mesh;

    [SerializeField]
    Variables variables;

    [SerializeField]
    Waves[] waves;

    List<Vector3> vertices;

    List<int> triangles;

    List<Vector2> uvs;


    [SerializeField]
    bool isWaved, Map, MountainedArea, Mountaines;

    private void Start()
    {
        mesh = new Mesh();
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
    }

    private void Update()
    {
        //JobHandle jobHandle = MeshGen();
        //jobHandle.Complete();

        GeneratingPlane();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
    }

    void GeneratingPlane()
    {
        vertices = new List<Vector3>();
        float xStep = variables.Dimension.x / variables.Resolution;
        float yStep = variables.Dimension.y / variables.Resolution;

        for (int z = 0; z < variables.Resolution + 1; z++)
        {
            for (int x = 0; x < variables.Resolution + 1; x++)
            {

                var y = 0f;

                //To get waves for the water use the below
                if (isWaved)
                {
                    for (int o = 0; o < waves.Length; o++)
                    {
                        if (waves[o].alternate)
                        {
                            var perl = Mathf.PerlinNoise((x * waves[o].scale.x) / variables.Dimension.x, (z * waves[o].scale.y) / variables.Dimension.y) * Mathf.PI * 2f; ;
                            y += Mathf.Cos(waves[o].speed.magnitude * Time.deltaTime) * waves[o].height;
                        }
                        else
                        {
                            var perl = Mathf.PerlinNoise((x * waves[o].scale.x + Time.time * waves[o].speed.x) / variables.Dimension.x, (z * waves[o].scale.y + Time.time * waves[o].speed.y) / variables.Dimension.y) - 0.5f;
                            y += perl * waves[o].height;
                        }
                    }
                }

                //To get an area or a map

                if (Map)
                {

                    float c = Mathf.PerlinNoise(xStep * variables.Seed, yStep * variables.Seed) * variables.Height;
                    float c2 = Mathf.PerlinNoise(x * variables.Seed + Mathf.PI, z * variables.Seed + Mathf.PI) * variables.Height * 0.5f;
                    float c3 = Mathf.PerlinNoise(x * variables.Seed / Mathf.PI, z * variables.Seed / Mathf.PI) * variables.Height / 0.5f;
                    float c4 = Mathf.PerlinNoise(x * variables.Seed * Mathf.PI, z * variables.Seed * Mathf.PI) * variables.Height - 0.1f;

                    //in case you want to get mounatins with more walkwable area
                    if (MountainedArea)
                    {
                        c += Mathf.Pow((c2 * c3 * c4), 3);
                    }
                    //to get a mounatines only for a back view
                    else if (Mountaines)
                    {
                        c *= c2 * +c3 * c4;
                    }
                    //for something like a forest or a jungle area
                    else
                    {
                        c += c2 + c3 + c4;
                    }
                    
                    //falloff map
                    //use falloff map when you gonna use the map code

                    float fallX = x / variables.Dimension.x * 2 - 1;
                    float fallZ = z / variables.Dimension.y * 2 - 1;

                    float t = Mathf.Max(Mathf.Abs(fallX), Mathf.Abs(fallZ));

                    if (t < variables.falloffStart)
                    {
                        y = c;
                    }
                    else if (t > variables.falloffEnd)
                    {
                        y = 0;
                    }
                    else
                    {
                        y = Mathf.SmoothStep(c, 0, Mathf.InverseLerp(variables.falloffStart, variables.falloffEnd, t));
                    }
                }

                vertices.Add(new Vector3(x * xStep, y, z * yStep));
            }
        }

        triangles = new List<int>();
        for (int r = 0; r < variables.Resolution; r++)
        {
            for (int c = 0; c < variables.Resolution; c++)
            {
                int i = (r * variables.Resolution) + r + c;

                triangles.Add(i);
                triangles.Add(i + (variables.Resolution) + 1);
                triangles.Add(i + (variables.Resolution) + 2);

                triangles.Add(i);
                triangles.Add(i + (variables.Resolution) + 2);
                triangles.Add(i + 1);
            }
        }

        uvs = new List<Vector2>();
        for (int x = 0; x <= variables.Resolution; x++)
        {
            for (int z = 0; z <= variables.Resolution; z++)
            {
                var vec = new Vector2((x / variables.UVScale) % 2, (z / variables.UVScale) % 2);
                uvs.Add(new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x, vec.y <= 1 ? vec.y : 2 - vec.y));
            }
        }
    }

    //private JobHandle MeshGen()
    //{
    //    MeshGenerator generator = new MeshGenerator
    //    {
    //        Vertices = vertices,
    //        Triangles = triangles,
    //        Variables = variables,
    //        Octaves = octaves
    //    };

    //    return generator.Schedule();
    //}
}

//public struct MeshGenerator : IJob
//{
//    public List<Vector3> Vertices;

//    public List<int> Triangles;

//    public Variables Variables;

//    public Octave[] Octaves;

//    public void Execute()
//    {
//        Vertices = new List<Vector3>();
//        float xStep = Variables.Dimension.x / Variables.Resolution;
//        float yStep = Variables.Dimension.y / Variables.Resolution;
//        for (int z = 0; z < Variables.Resolution + 1; z++)
//        {
//            for (int x = 0; x < Variables.Resolution + 1; x++)
//            {
//                var y = 0f;
//                for (int o = 0; o < Octaves.Length; o++)
//                {
//                    if (Octaves[o].alternate)
//                    {
//                        var perl = Mathf.PerlinNoise((x * Octaves[o].scale.x) / Variables.Dimension.x, (z * Octaves[o].scale.y) / Variables.Dimension.y) * Mathf.PI * 2f; ;
//                        y += Mathf.Cos(Octaves[o].speed.magnitude * Time.deltaTime) * Octaves[o].height /** variables.curve.Evaluate(Time.deltaTime)*/;
//                    }
//                    else
//                    {
//                        var perl = Mathf.PerlinNoise((x * Octaves[o].scale.x + Time.time * Octaves[o].speed.x) / Variables.Dimension.x, (z * Octaves[o].scale.y + Time.time * Octaves[o].speed.y) / Variables.Dimension.y) - 0.5f;
//                        y += perl * Octaves[o].height/* * variables.curve.Evaluate(Time.deltaTime)*/;
//                    }
//                }
//                Vertices.Add(new Vector3(x * xStep, y, z * yStep));
//            }
//        }

//        Triangles = new List<int>();
//        for (int r = 0; r < Variables.Resolution; r++)
//        {
//            for (int c = 0; c < Variables.Resolution; c++)
//            {
//                int i = (r * Variables.Resolution) + r + c;

//                Triangles.Add(i);
//                Triangles.Add(i + (Variables.Resolution) + 1);
//                Triangles.Add(i + (Variables.Resolution) + 2);

//                Triangles.Add(i);
//                Triangles.Add(i + (Variables.Resolution) + 2);
//                Triangles.Add(i + 1);
//            }
//        }
//    }
//}