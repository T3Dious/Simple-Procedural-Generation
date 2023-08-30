using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System;
using Noises;

public class MeshGeneration : MonoBehaviour
{
    MeshFilter meshFilter;

    Mesh mesh;

    public Variable variable;

    public bool Mountaines;

    List<Vector3> vertices;

    List<int> triangles;

    List<Vector2> uvs;

    public float frequency = 1, maxHeight = 16, minHeight = 1;

    private void Start()
    {
        mesh = new Mesh();
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        
    }

    private void Update()
    {
        Map();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    //sorry i know this function maybe look a bit complicated but i will do my best to demonstrate them
    void Map()
    {
        //float xStep = variable.Dimension.x / variable.Resolution;
        //float zStep = variable.Dimension.y / variable.Resolution;
        vertices = new List<Vector3>();
        for (int x = 0; x < variable.Resolution + 1; x++) //looping over the z axis in every x axis
        {
            for (int z = 0; z < variable.Resolution + 1; z++) //looping over x axises in every z axis
            {
                float y = 0;
                y = NoiseHeight(x, z);

                vertices.Add(new Vector3(x, y, z));
            }
        }

        triangles = new List<int>();
        for (int r = 0; r < variable.Resolution; r++)
        {
            for (int c = 0; c < variable.Resolution; c++)
            {
                int i = (r * variable.Resolution) + r + c;

                triangles.Add(i);
                triangles.Add(i + (variable.Resolution) + 1);
                triangles.Add(i + (variable.Resolution) + 2);

                triangles.Add(i);
                triangles.Add(i + (variable.Resolution) + 2);
                triangles.Add(i + 1);
            }
        }

        uvs = new List<Vector2>();
        for (int x = 0; x <= variable.Resolution; x++)
        {
            for (int z = 0; z <= variable.Resolution; z++)
            {
                var vec = new Vector2((x / variable.UVScale) % 2, (z / variable.UVScale) % 2);
                uvs.Add(new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x, vec.y <= 1 ? vec.y : 2 - vec.y));
            }
        }
    }

    float NoiseHeight(float sizeX, float sizeZ)
    {
        var y = 0f; //to store the final evaluation of the heights that resulted from the noises
        var noise = 0f; //to store the final value of noise value that generated from FBM
        float Difftime = variable.diff * (2 / variable.omega); //you can search about simple harmonic motion, but the edit here that i replaced pi value 
                                                               //with diff value to control the space between peeks to get an area or a map
        noise = FractalBrownianMotion(sizeX * variable.xOffset, sizeZ * variable.zOffset) * variable.amplitude; //calculating the noise value
        float Yx = variable.MainWaves * variable.omega * variable.alpha * Mathf.Sin(sizeX * variable.omega * math.PI * Difftime)
            + Mathf.Sin(variable.PartialWaves * sizeX * variable.omega * math.PI * Difftime) * noise, // you can search about sine ntimes wave and then look for multiple waves,
                                                                                                  // one of the equations i use is nth waves with frequencies with the main number of waves(MainWaves) and partial number of waves(Partial Waves)
                                                                                                  // (second waves or waves inside the maines waves)
            Yz = variable.MainWaves * variable.omega * variable.alpha * Mathf.Sin(sizeZ * variable.omega * math.PI * Difftime)
            - Mathf.Sin(variable.PartialWaves * sizeZ * variable.omega * math.PI * Difftime) * noise;
        //very important point
        //alpha and omega values here approximately simulates the erosion hydraulic process (it is not erosion hydraulic) but it is gives a detials,
        //near to it. MainWaves and PartialWaves play a role too.
        //it is not the best approach but can do for low budget games like what am trying to make^^
        float c = FractalBrownianMotion(Yx, Yz) * noise * frequency,
            c2 = FractalBrownianMotion(Yx, Yz) * noise * frequency,
            c3 = FractalBrownianMotion(Yx, Yz) * noise * frequency,
            c4 = FractalBrownianMotion(Yx, Yz) * noise * frequency; // more than value of a more detailed layers in the noise final layer

        if (Mountaines) //to get a mounatines only for a back view
            c = c2 * c3 * c4; //it is just to give less space between the peeks

        else //for something like a forest or a jungle area
            c = c2 + c3 + c4; //normal way to calculate a ground

        //falloff map
        //for a more detailing shore view
        float fallX = sizeX / variable.Resolution * 2 - 1;
        float fallZ = sizeZ / variable.Resolution * 2 - 1;
        float t = Mathf.Max(Mathf.Abs(fallX), Mathf.Abs(fallZ));

        if (t < variable.falloffStart)
            y = c;
        else if (t > variable.falloffEnd)
            y = 0;
        else
            y = Mathf.Clamp(c, 0, Mathf.InverseLerp(variable.falloffStart, variable.falloffEnd, t));

        return y;
    }

    public float FractalBrownianMotion(float x, float y)
    {
        //next values is just to store the values
        float FBMamplitude = maxHeight;
        float elevation = 0;
        var t_frequency = frequency;
        var t_amplitude = FBMamplitude;
        //this loop is for to get more than 1 layer of perlin noise then combine them all to get a single noise layer (FBM)
        for (int o = 0; o < variable.Octaves; o++)
        {
            var sampleX = x * t_frequency; //width of the plane multiplied by the frequency
            var sampleZ = y * t_frequency; //depth of the plane multiplied by the frequency
            elevation += Mathf.PerlinNoise(sampleX * t_frequency, sampleZ * t_frequency) * t_amplitude;//width and depth multiplied by the frequency
            //for more detailed noise, multiplied by the amplitude to control its height
            t_frequency *= variable.lacuranity;//for increasing the number of waves
            t_amplitude *= variable.persistence;//for contorling the height of waves
        }
        //this to get more smooth shape of the elevtion (noise layer) to get a better shape of the heights 
        elevation = math.clamp(elevation, -maxHeight, minHeight);
        return elevation;
    }

    Vector2 random2(Vector2 p)
    {
        return math.frac(math.sin(new Vector2(Vector2.Dot(p, new Vector2(127.1f, 311.7f)), Vector2.Dot(p, new Vector2(269.5f, 183.3f)))) * 43758.5453f);
    }

}

[System.Serializable]
public struct Variable
{
    [Range(10, 1024)] public int Resolution;
    public int Octaves;
    public float UVScale;
    public float lacuranity, persistence;
    public float falloffStart, falloffEnd;
    public float xOffset, zOffset, amplitude;
    public float omega, diff, alpha;
    public float MainWaves, PartialWaves;
}
