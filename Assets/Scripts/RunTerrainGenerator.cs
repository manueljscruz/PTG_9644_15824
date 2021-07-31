using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using System;

public class RunTerrainGenerator : MonoBehaviour
{
    public bool checkEnd;
    public bool runThreads;

    // Parameters
    [Header("Terrains")]
    public Terrain noThreadTerrain;
    public Terrain threadTerrain;
    public TerrainData noThreadTerrainData;
    public TerrainData threadTerrainData;
    
    [Header("Operations")]
    public float currentTime;
    public Algorithm operationAlgorithm;

    [Header("Random Heights")]
    [SerializeField] [Range(0, 1)] float rndMinHeightRange;
    [SerializeField] [Range(0, 1)] float rndMaxHeightRange;

    [Header("Heightmap")]
    public Texture2D heightMapImage;
    [SerializeField] Vector3 heightmapscale;

    [Header("Perlin Noise")]
    [SerializeField] float perlinXScale = 0.01f;
    [SerializeField] float perlinYScale = 0.01f;
    [SerializeField] int perlinOffsetX = 0;
    [SerializeField] int perlinOffsetY = 0;
    [SerializeField] int perlinOctaves = 3;
    [SerializeField] float perlinPersistance = 8.0f;
    [SerializeField] float perlinHeightScale = 0.09f;

    [Header("Mid Point Displacement")]
    [SerializeField] float midpointHeightMin = -2f;
    [SerializeField] float midpointHeightMax = 2f;
    [SerializeField] float midpointHeightDampener = 2f;
    [SerializeField] float midpointRoughness = 2f;

    
    [System.Serializable] public class SplatHeights
    {
        public Texture2D texture = null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public Vector2 tileOffset = new Vector2(0, 0);
        public Vector2 tileSize = new Vector2(50, 50);
    }

    [Header("Splat Maps")]
    public List<SplatHeights> splatHeights = new List<SplatHeights>()
    {
        new SplatHeights()
    };

    // Cached
    #region Thread Setup Related
    private int numberOfLogicalProcessors;
    private int numberOfWorkerThreads;
    private int numberOfIOCPThreads;
    private int maxAllowedThreads;

    Thread[] threads;
    #endregion

    #region Random Heights
    private Vector2 RandomHeightRanges;
    #endregion

    // States
    enum OpStatus
    {
        Idle,
        Running,
        Finnished
    }

    public enum Algorithm
    {
        Heightmaps,
        Perlin,
        Perlin_FBM,
        MidpointDisplacement
    }

    private OpStatus currentOpStatus;
    

    // Start is called before the first frame update
    private void Awake()
    {
        ResetTerrain();
        currentOpStatus = OpStatus.Idle;

        numberOfLogicalProcessors = System.Environment.ProcessorCount;
        ThreadPool.GetMinThreads(out numberOfWorkerThreads, out numberOfIOCPThreads);
        ThreadingTerrain.SetupThreading(numberOfLogicalProcessors, numberOfWorkerThreads, numberOfIOCPThreads, threadTerrainData.heightmapResolution, threadTerrainData.alphamapWidth, threadTerrainData.alphamapHeight);
        SetMaxThreads(numberOfWorkerThreads);
    }

    void Start()
    {
        RandomHeightRanges = new Vector2(rndMinHeightRange, rndMaxHeightRange);
        checkEnd = true;
    }

    // Update is called once per frame
    void Update()
    {
        currentTime += Time.deltaTime;

        if (currentOpStatus == OpStatus.Idle)
        {
            currentOpStatus = OpStatus.Running;
            checkEnd = true;
            RunTaskListAsync();
        }

        else if (currentOpStatus == OpStatus.Finnished && checkEnd == true)
        {
            Debug.Log("All Done");
            checkEnd = false;
        }
    }

    #region Methods

    /// <summary>
    /// Return Height Maps in a float array
    /// </summary>
    /// <param name="terrainData"></param>
    /// <returns></returns>
    float[,] GetHeightMap(TerrainData terrainData)
    {
        return terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
    }

    #endregion

    #region Run Task List

    async Task RunTaskListAsync()
    {
        try
        {
            float[,] heightMapNoThreads = new float[noThreadTerrainData.heightmapResolution, noThreadTerrainData.heightmapResolution];
            float[,] heightMapThreaded = new float[threadTerrainData.heightmapResolution, threadTerrainData.heightmapResolution];
            float[,] greyscaleValuesNoThreads = GetGreyscaleValues(noThreadTerrainData, heightMapImage, heightmapscale);
            float[,] greyscaleValuesThreaded = GetGreyscaleValues(threadTerrainData, heightMapImage, heightmapscale);
            float[,,] splatMapsNoThreads = new float[noThreadTerrainData.alphamapWidth, noThreadTerrainData.alphamapHeight, noThreadTerrainData.alphamapLayers];
            // float[,,] splatMapsThreaded = new float[threadTerrainData.alphamapWidth, threadTerrainData.alphamapHeight, threadTerrainData.alphamapLayers];
            threadTerrainData.splatPrototypes = null;

            SplatPrototype[] newSplatPrototypes;
            newSplatPrototypes = new SplatPrototype[splatHeights.Count];
            int spindex = 0;
            foreach (SplatHeights sh in splatHeights)
            {
                newSplatPrototypes[spindex] = new SplatPrototype();
                newSplatPrototypes[spindex].texture = sh.texture;
                newSplatPrototypes[spindex].tileOffset = sh.tileOffset;
                newSplatPrototypes[spindex].tileSize = sh.tileSize;
                newSplatPrototypes[spindex].texture.Apply(true);
                spindex++;
            }

            noThreadTerrainData.splatPrototypes = newSplatPrototypes;
            // threadTerrainData.splatPrototypes = newSplatPrototypes;

            await WaitDuration(2);

            // No threadss
            switch (operationAlgorithm)
            {
                case Algorithm.Heightmaps:
                    heightMapNoThreads = ThreadingTerrain.LoadTexture(noThreadTerrainData, greyscaleValuesNoThreads, heightmapscale);
                    break;

                case Algorithm.Perlin:
                    heightMapNoThreads = ThreadingTerrain.BasicPerlin(noThreadTerrainData, perlinOffsetX, perlinXScale, perlinOffsetY, perlinYScale);
                    break;

                case Algorithm.Perlin_FBM:
                    heightMapNoThreads = ThreadingTerrain.PerlinFBM(noThreadTerrainData, perlinOffsetX, perlinXScale, perlinOffsetY, perlinYScale, perlinOctaves, perlinPersistance, perlinHeightScale);
                    break;

                case Algorithm.MidpointDisplacement:
                    heightMapNoThreads = ThreadingTerrain.MidPointDisplacement(noThreadTerrainData, midpointHeightMin, midpointHeightMax, midpointHeightDampener, midpointRoughness);
                    break;
            }

            noThreadTerrainData.SetHeights(0, 0, heightMapNoThreads);

            splatMapsNoThreads = ThreadingTerrain.ApplySplatMaps(noThreadTerrainData, splatHeights);

            noThreadTerrainData.SetAlphamaps(0, 0, splatMapsNoThreads);


            // Threads
            if (runThreads)
            {
                threads = new Thread[maxAllowedThreads];

                await WaitDuration(2);

                for (int i = 0; i < threads.Length; i++)
                {
                    switch (operationAlgorithm)
                    {
                        case Algorithm.Heightmaps:
                            threads[i] = new Thread(() => ThreadingTerrain.LoadTextureThreaded(heightMapThreaded, out heightMapThreaded, i, greyscaleValuesThreaded, heightmapscale));
                            break;

                        case Algorithm.Perlin:
                            threads[i] = new Thread(() => ThreadingTerrain.BasicPerlinThreaded(heightMapThreaded, out heightMapThreaded, i, perlinOffsetX, perlinXScale, perlinOffsetY, perlinYScale));
                            break;

                        case Algorithm.Perlin_FBM:
                            threads[i] = new Thread(() => ThreadingTerrain.PerlinFBMThreaded(heightMapThreaded, out heightMapThreaded, i, perlinOffsetX, perlinXScale, perlinOffsetY, perlinYScale, perlinOctaves, perlinPersistance, perlinHeightScale));
                            break;

                        case Algorithm.MidpointDisplacement:

                            break;
                    }

                    threads[i].Start();
                }

                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Join();
                }


                threadTerrainData.SetHeights(0, 0, heightMapThreaded);

            }


            currentOpStatus = OpStatus.Finnished;
        }
        catch(Exception ex)
        {

        }
        
    }

    #endregion

    // Core

    #region Reset Terrain

    /// <summary>
    /// Reset Both Terrains
    /// </summary>
    public void ResetTerrain()
    {
        // No Threaded Terrain Data
        float[,] heightMap = GetHeightMap(noThreadTerrainData);
        for (int x = 0; x < noThreadTerrainData.heightmapResolution; x++)
        {
            for (int z = 0; z < noThreadTerrainData.heightmapResolution; z++)
            {
                heightMap[x, z] = 0;
            }
        }
        // Set height maps starting at position 0 0
        noThreadTerrainData.SetHeights(0, 0, heightMap);
        threadTerrainData.SetHeights(0, 0, heightMap);
    }

    #endregion

    #region Set Max Threads

    /// <summary>
    /// Setup the max number of threads
    /// </summary>
    /// <param name="noWorkerThreads"></param>
    public void SetMaxThreads(int noWorkerThreads)
    {
        if (noWorkerThreads == 1) maxAllowedThreads = 1;
        else if (noWorkerThreads > 1 && noWorkerThreads < 4) maxAllowedThreads = 2;
        else if (noWorkerThreads >= 4 && noWorkerThreads < 8) maxAllowedThreads = 4;
        else if (noWorkerThreads >= 8 && noWorkerThreads < 16) maxAllowedThreads = 8;
        else maxAllowedThreads = 16;
    }

    #endregion

    // Functions

    /// <summary>
    /// Get Greyscale values of image
    /// </summary>
    /// <param name="terrainData"></param>
    /// <param name="texture"></param>
    /// <param name="heightMapScale"></param>
    /// <returns></returns>
    public float[,] GetGreyscaleValues(TerrainData terrainData, Texture2D texture, Vector3 heightMapScale)
    {
        float[,] greyScaleValues = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];

        for(int z = 0; z < terrainData.heightmapResolution; z++)
        {
            for(int x = 0; x < terrainData.heightmapResolution; x++)
            {
                greyScaleValues[x, z] = texture.GetPixel((int)(x * heightMapScale.x), (int)(z * heightMapScale.z)).grayscale;
            }
        }

        return greyScaleValues;
    }


    private async Task WaitDuration(double interval)
    {
        await Task.Delay(TimeSpan.FromSeconds(interval));
        Debug.Log("Finished waiting.");
    }
}
