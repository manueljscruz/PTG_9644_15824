using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static RunTerrainGenerator;

public class ThreadingTerrain
{
    static int numberOfLogicalProcessors = 0;
    static int noWorkerThreads = 0;
    static int noIOCPThreads = 0;
    static int threadedTerrainResolution = 0;
    static int threadedAlphaMapWidth = 0;
    static int threadedAlphaMapHeight = 0;
    static int maxWorkerThreads = 0;
    static int rowCount = 0;
    static int colCount = 0;

    public static bool resetTerrain = true;

    #region Setup Threading

    /// <summary>
    /// Setup the number of processors, workers and iocp counts
    /// </summary>
    /// <param name="processorCount"></param>
    /// <param name="workerCount"></param>
    /// <param name="iocpCount"></param>
    public static void SetupThreading(int processorCount, int workerCount, int iocpCount, int resolution, int alphaMapWidth, int alphaMapHeight)
    {
        numberOfLogicalProcessors = processorCount;
        noWorkerThreads = workerCount;
        noIOCPThreads = iocpCount;
        threadedTerrainResolution = resolution;
        threadedAlphaMapWidth = alphaMapWidth;
        threadedAlphaMapHeight = alphaMapHeight;

        if (noWorkerThreads == 1) maxWorkerThreads = 1;
        else if (noWorkerThreads > 1 && noWorkerThreads < 4) maxWorkerThreads = 2;
        else if (noWorkerThreads >= 4 && noWorkerThreads < 8) maxWorkerThreads = 4;
        else if (noWorkerThreads >= 8 && noWorkerThreads < 16) maxWorkerThreads = 8;
        else maxWorkerThreads = 16;

        SetRowAndColThreads(maxWorkerThreads);
    }

    #endregion

    #region Random Terrain Heights

    public static void RandomTerrainThreaded(out float[,] outTerrainData, int threadNo, float minHeight, float maxHeight)
    {
        outTerrainData = new float[threadedTerrainResolution, threadedTerrainResolution];

        int minX = 0;
        int maxX = 0;
        int minY = 0;
        int maxY = 0;

        ReturnIntervals(threadNo, out minX, out maxX, out minY, out maxY);

        for(int y = minY; y < maxY; y++)
        {
            for(int x = minX; x < maxX; x++)
            {
                outTerrainData[x, y] += GetRandomFloatNumber(minHeight, maxHeight);
            }
        }
    }

    #endregion

    #region Load Texture Terrain

    public static float[,] LoadTexture(TerrainData terrainData, float[,] greyscaleValues, Vector3 heightMapScale)
    {
        float[,] heightMap = new float[terrainData.heightmapResolution,terrainData.heightmapResolution];
        for (int x = 0; x < terrainData.heightmapResolution; x++)

        {
            for (int z = 0; z < terrainData.heightmapResolution; z++)
            {
                // the multiplication by the scale is to stretch to the size of the terrain
                heightMap[x, z] = greyscaleValues[x,z] * heightMapScale.y;
            }
        }

        return heightMap;
    }

    public static void LoadTextureThreaded(float[,] terrainData, out float[,] outTerrainData, int threadNo, float[,] greyscaleValues, Vector3 heightMapScale)
    {
        outTerrainData = terrainData;

        int minX = 0;
        int maxX = 0;
        int minY = 0;
        int maxY = 0;

        ReturnIntervals(threadNo, out minX, out maxX, out minY, out maxY);

        Debug.Log(string.Format("Thread NO {4} - X Min: {0}  X Max : {1} Y Min: {2}  Y Max : {3}", minX, maxX, minY, maxY, threadNo));

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                // the multiplication by the scale is to stretch to the size of the terrain
                outTerrainData[x, y] = greyscaleValues[x, y] * heightMapScale.y;
            }
        }
    }

    #endregion

    #region Basic Perlin

    public static float[,] BasicPerlin(TerrainData terrainData, int perlinOffsetX, float perlinXScale, int perlinOffsetY, float perlinYScale)
    {
        float[,] heightMap = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                heightMap[x, y] = Mathf.PerlinNoise((x + perlinOffsetX) * perlinXScale, (y + perlinOffsetY) * perlinYScale);
            }
        }

        return heightMap;
    }

    public static void BasicPerlinThreaded(float[,] terrainData, out float[,] outTerrainData, int threadNo, int perlinOffsetX, float perlinXScale, int perlinOffsetY, float perlinYScale)
    {
        outTerrainData = terrainData;

        int minX = 0;
        int maxX = 0;
        int minY = 0;
        int maxY = 0;

        ReturnIntervals(threadNo, out minX, out maxX, out minY, out maxY);

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                outTerrainData[x, y] = Mathf.PerlinNoise((x + perlinOffsetX) * perlinXScale, (y + perlinOffsetY) * perlinYScale);
            }
        }
    }

    #endregion

    #region Perlin FBM

    public static float[,] PerlinFBM(TerrainData terrainData, int perlinOffsetX, float perlinXScale, int perlinOffsetY, float perlinYScale, int perlinOctaves, float perlinPersistance, float perlinHeightScale)
    {
        float[,] heightMap = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                heightMap[x, y] = fBM((x + perlinOffsetX) * perlinXScale,
                                            (y + perlinOffsetY) * perlinYScale,
                                            perlinOctaves,
                                            perlinPersistance) * perlinHeightScale;
            }
        }

        return heightMap;
    }

    public static void PerlinFBMThreaded(float[,] terrainData, out float[,] outTerrainData, int threadNo, int perlinOffsetX, float perlinXScale, int perlinOffsetY, float perlinYScale, int perlinOctaves, float perlinPersistance, float perlinHeightScale)
    {
        outTerrainData = terrainData;

        int minX = 0;
        int maxX = 0;
        int minY = 0;
        int maxY = 0;

        ReturnIntervals(threadNo, out minX, out maxX, out minY, out maxY);

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                outTerrainData[x, y] = fBM((x + perlinOffsetX) * perlinXScale, (y + perlinOffsetY) * perlinYScale, perlinOctaves, perlinPersistance) * perlinHeightScale;
            }
        }
    }

    #endregion

    #region Midpoint Displacement

    public static float[,] MidPointDisplacement(TerrainData terrainData, float midpointHeightMin, float midpointHeightMax, float midpointHeightDampener, float midpointRoughness)
    {
        float[,] heightMap = new float[terrainData.heightmapResolution,terrainData.heightmapResolution];

        int width = threadedTerrainResolution - 1;     // Width
        int squareSize = width;                         // Size of the square

        float heightMin = midpointHeightMin;  // (float)squareSize / 2.0f * 0.01f;
        float heightMax = midpointHeightMax;
        float heightDampener = (float)Mathf.Pow(midpointHeightDampener, -1 * midpointRoughness);

        int cornerX, cornerY;                           // Opposite corner of the square
        int midX, midY;                                 // Mid point of the square
        int pmidXL, pmidXR, pmidYU, pmidYD;             // Mid X on the left, Mid X on the right, Mid Y on the upper, Mid Y on the down

        while (squareSize > 0)
        {
            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);    // Mid point
                    midY = (int)(y + squareSize / 2.0f);    // Mid Point

                    heightMap[midX, midY] = (float)((heightMap[x, y] +
                                                    heightMap[cornerX, y] +
                                                    heightMap[x, cornerY] +
                                                    heightMap[cornerX, cornerY]) / 4.0f +
                                                    UnityEngine.Random.Range(heightMin, heightMax));
                }
            }

            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);    // Mid point
                    midY = (int)(y + squareSize / 2.0f);    // Mid Point

                    pmidXR = (int)(midX + squareSize);
                    pmidYU = (int)(midY + squareSize);
                    pmidXL = (int)(midX - squareSize);
                    pmidYD = (int)(midY - squareSize);

                    if (pmidXL <= 0 || pmidYD <= 0 || pmidXR >= width - 1 || pmidYU >= width - 1) continue;

                    // Square value of the bottom side
                    heightMap[midX, y] = (float)((heightMap[midX, midY] +
                                                    heightMap[x, y] +
                                                    heightMap[midX, pmidYD] +
                                                    heightMap[cornerX, y]) / 4.0f +
                                                    UnityEngine.Random.Range(heightMin, heightMax));

                    // Square Value of the top side
                    heightMap[midX, cornerY] = (float)((heightMap[x, cornerY] +
                                                    heightMap[midX, midY] +
                                                    heightMap[cornerX, cornerY] +
                                                    heightMap[midX, pmidYU]) / 4.0f +
                                                    UnityEngine.Random.Range(heightMin, heightMax));

                    // Square value of the left side
                    heightMap[x, midY] = (float)((heightMap[x, y] +
                                                    heightMap[pmidXL, midY] +
                                                    heightMap[x, cornerY] +
                                                    heightMap[midX, midY]) / 4.0f +
                                                    UnityEngine.Random.Range(heightMin, heightMax));

                    // Square value of the right side
                    heightMap[cornerX, midY] = (float)((heightMap[midX, y] +
                                                    heightMap[midX, midY] +
                                                    heightMap[cornerX, cornerY] +
                                                    heightMap[pmidXR, midY]) / 4.0f +
                                                    UnityEngine.Random.Range(heightMin, heightMax));
                }
            }

            squareSize = (int)(squareSize / 2.0f);
            heightMin *= heightDampener;
            heightMax *= heightDampener;
        }

        return heightMap;
    }

    #endregion

    #region Splat Maps

    public static float[,,] ApplySplatMaps(TerrainData terrainData, List<SplatHeights> splatHeights)
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                float[] splat = new float[terrainData.alphamapLayers];
                // Loop through our textures
                for (int i = 0; i < splatHeights.Count; i++)
                {
                    // Get the applicable heights for this texture
                    float thisHeightStart = splatHeights[i].minHeight;
                    float thisHeightStop = splatHeights[i].maxHeight;

                    // if current position is within those heights
                    if ((heightMap[x, y] >= thisHeightStart && heightMap[x, y] <= thisHeightStop))
                    {
                        splat[i] = 1;   // apply
                    }
                    // Normalize splat values so that the sum of all splat maps, become 1
                    splat = NormalizeVector(splat);
                    for (int j = 0; j < splatHeights.Count; j++)
                    {
                        splatmapData[x, y, j] = splat[j];
                    }
                }
            }
        }

        return splatmapData;
    }

    /// <summary>
    /// Not Working
    /// </summary>
    /// <param name="splatMapData"></param>
    /// <param name="outSplatMapData"></param>
    /// <param name="heightMap"></param>
    /// <param name="threadNo"></param>
    /// <param name="splatHeights"></param>
    /// <param name="alphaLayerCount"></param>
    public static void ApplySplatMapsThreaded(float[,,] splatMapData, out float[,,] outSplatMapData, float[,] heightMap, int threadNo, List<SplatHeights> splatHeights, int alphaLayerCount)
    {
        outSplatMapData = splatMapData;
        int minX = 0;
        int maxX = 0;
        int minY = 0;
        int maxY = 0;

        ReturnAlphaIntervals(threadNo, out minX, out maxX, out minY, out maxY);

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                float[] splat = new float[alphaLayerCount];
                // Loop through our textures
                for (int i = 0; i < splatHeights.Count; i++)
                {
                    // Get the applicable heights for this texture
                    float thisHeightStart = splatHeights[i].minHeight;
                    float thisHeightStop = splatHeights[i].maxHeight;

                    // if current position is within those heights
                    if ((heightMap[x, y] >= thisHeightStart && heightMap[x, y] <= thisHeightStop))
                    {
                        splat[i] = 1;   // apply
                    }
                    // Normalize splat values so that the sum of all splat maps, become 1
                    splat = NormalizeVector(splat);
                    for (int j = 0; j < splatHeights.Count; j++)
                    {
                        outSplatMapData[x, y, j] = splat[j];
                    }
                }
            }
        }

    }

    #endregion

    // AUX

    #region Get Random Float Number

    /// <summary>
    /// Get Random Float number (Not sure if it works)
    /// </summary>
    /// <param name="minimum"></param>
    /// <param name="maximum"></param>
    /// <returns></returns>
    public static float GetRandomFloatNumber(float minimum, float maximum)
    {
        System.Random rnd = new System.Random();
        return (float)rnd.NextDouble() * (maximum - minimum) + minimum;
    }

    #endregion

    #region Fractal Brownian Motion

    public static float fBM(float x, float y, int oct, float persistance)
    {
        float total = 0;            // Total height count
        float frequency = 1;        // frequency -> how close are the waves together
        float amplitude = 1;        // 
        float maxValue = 0;         // adition of each amplitude by octave

        for (int i = 0; i < oct; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistance;
            frequency *= 2;
        }

        return total / maxValue;
    }

    #endregion

    #region Normalize Vectors
    private static float[] NormalizeVector(float[] v)
    {
        float total = 0;
        for (int i = 0; i < v.Length; i++)
        {
            total += v[i];
        }
        for (int i = 0; i < v.Length; i++)
        {
            v[i] /= total;
        }
        return v;
    }
    #endregion

    // Core

    #region Core
    private static void SetRowAndColThreads(int maxNoThreadsAllowed)
    {
        switch (maxNoThreadsAllowed)
        {
            case 1:
                colCount = 1;
                rowCount = 1;
                break;

            case 2:
                colCount = 1;
                rowCount = 2;
                break;

            case 4:
                colCount = 2;
                rowCount = 2;
                break;

            case 6:
                colCount = 3;
                rowCount = 2;
                break;

            case 8:
                colCount = 4;
                rowCount = 2;
                break;

            case 16:
                colCount = 4;
                rowCount = 4;
                break;
        }
    } 

    private static void ReturnIntervals(int thread, out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = 0;
        maxX = 0;
        minY = 0;
        maxY = 0;

        int partX = (threadedTerrainResolution - 1) / rowCount;
        int partY = (threadedTerrainResolution - 1) / colCount;

        #region 1 Threads
        if (maxWorkerThreads == 1)
        {
            minX = 0;
            maxX = partX;
            minY = 0;
            maxY = partY;
        }
        #endregion

        #region 2 Threads
        else if (maxWorkerThreads == 2)
        {
            switch (thread)
            {
                case 0:
                    minX = 0;
                    maxX = partX;
                    minY = 0;
                    maxY = partY;
                    break;

                case 1:
                    minX = 0;
                    maxX = partX;
                    minY = partY;
                    maxY = partY * 2;
                    break;
            }
        }
        #endregion

        #region 4 Threads
        else if (maxWorkerThreads == 4)
        {
            switch (thread)
            {
                case 0:
                    minX = 0;
                    maxX = partX;
                    minY = 0;
                    maxY = partY;
                    break;

                case 1:
                    minX = partX;
                    maxX = threadedTerrainResolution;
                    minY = partY;
                    maxY = threadedTerrainResolution;
                    break;

                case 2:
                    minX = 0;
                    maxX = partX;
                    minY = partY;
                    maxY = threadedTerrainResolution;
                    break;

                case 3:
                    minX = partX;
                    maxX = threadedTerrainResolution;
                    minY = partY;
                    maxY = threadedTerrainResolution;
                    break;
            }
        }
        #endregion

        #region 8 Threads
        else if(maxWorkerThreads == 8)
        {
            switch (thread)
            {
                case 0:
                    minX = 0;
                    maxX = partX;
                    minY = 0;
                    maxY = partY;
                    break;

                case 1:
                    minX = partX;
                    maxX = partX * 2;
                    minY = 0;
                    maxY = partY;
                    break;

                case 2:
                    minX = partX * 2;
                    maxX = partX * 3;
                    minY = 0;
                    maxY = partY;
                    break;

                case 3:
                    minX = partX * 3;
                    maxX = threadedTerrainResolution;
                    minY = 0;
                    maxY = partY;
                    break;

                case 4:
                    minX = 0;
                    maxX = partX;
                    minY = partY;
                    maxY = threadedTerrainResolution;
                    break;

                case 5:
                    minX = partX;
                    maxX = partX * 2;
                    minY = partY;
                    maxY = threadedTerrainResolution;
                    break;

                case 6:
                    minX = partX * 2;
                    maxX = partX * 3;
                    minY = partY;
                    maxY = threadedTerrainResolution;
                    break;

                case 7:
                    minX = partX * 3;
                    maxX = threadedTerrainResolution;
                    minY = partY;
                    maxY = threadedTerrainResolution;
                    break;
            }
        }


        #endregion

        #region 16 Threads
        else if (maxWorkerThreads == 16)
        {
            switch (thread)
            {
                #region 1st Row
                
                case 0:
                    minX = 0;
                    maxX = partX;
                    minY = 0;
                    maxY = partY;
                    break;

                case 1:
                    minX = partX;
                    maxX = partX * 2;
                    minY = 0;
                    maxY = partY;
                    break;

                case 2:
                    minX = partX * 2;
                    maxX = partX * 3;
                    minY = 0;
                    maxY = partY;
                    break;

                case 3:
                    minX = partX * 3;
                    maxX = threadedTerrainResolution;
                    minY = 0;
                    maxY = partY;
                    break;

                #endregion

                #region 2nd Row

                case 4:
                    minX = 0;
                    maxX = partX;
                    minY = partY;
                    maxY = partY * 2;
                    break;

                case 5:
                    minX = partX;
                    maxX = partX * 2;
                    minY = partY;
                    maxY = partY * 2;
                    break;

                case 6:
                    minX = partX * 2;
                    maxX = partX * 3;
                    minY = partY;
                    maxY = partY * 2;
                    break;

                case 7:
                    minX = partX * 3;
                    maxX = threadedTerrainResolution;
                    minY = partY;
                    maxY = partY * 2;
                    break;

                #endregion

                #region 3rd Row

                case 8:
                    minX = 0;
                    maxX = partX;
                    minY = partY * 2;
                    maxY = partY * 3;
                    break;

                case 9:
                    minX = partX;
                    maxX = partX * 2;
                    minY = partY * 2;
                    maxY = partY * 3;
                    break;

                case 10:
                    minX = partX * 2;
                    maxX = partX * 3;
                    minY = partY * 2;
                    maxY = partY * 3;
                    break;

                case 11:
                    minX = partX * 3;
                    maxX = threadedTerrainResolution;
                    minY = partY * 2;
                    maxY = partY * 3;
                    break;

                #endregion

                #region 4rd Row

                case 12:
                    minX = 0;
                    maxX = partX;
                    minY = partY * 3;
                    maxY = threadedTerrainResolution;
                    break;

                case 13:
                    minX = partX;
                    maxX = partX * 2;
                    minY = partY * 3;
                    maxY = threadedTerrainResolution;
                    break;

                case 14:
                    minX = partX * 2;
                    maxX = partX * 3;
                    minY = partY * 3;
                    maxY = threadedTerrainResolution;
                    break;

                case 15:
                    minX = partX * 3;
                    maxX = threadedTerrainResolution;
                    minY = partY * 3;
                    maxY = threadedTerrainResolution;
                    break;

                #endregion
            }
        }
        #endregion
    }

    private static void ReturnAlphaIntervals(int thread, out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = 0;
        maxX = 0;
        minY = 0;
        maxY = 0;

        int partX = (threadedAlphaMapWidth - 1) / rowCount;
        int partY = (threadedAlphaMapHeight - 1) / colCount;

        #region 1 Threads
        if (maxWorkerThreads == 1)
        {
            minX = 0;
            maxX = partX;
            minY = 0;
            maxY = partY;
        }
        #endregion

        #region 2 Threads
        else if (maxWorkerThreads == 2)
        {
            switch (thread)
            {
                case 0:
                    minX = 0;
                    maxX = partX;
                    minY = 0;
                    maxY = partY;
                    break;

                case 1:
                    minX = 0;
                    maxX = partX;
                    minY = partY;
                    maxY = partY * 2;
                    break;
            }
        }
        #endregion

        #region 4 Threads
        else if (maxWorkerThreads == 4)
        {
            switch (thread)
            {
                case 0:
                    minX = 0;
                    maxX = partX;
                    minY = 0;
                    maxY = partY;
                    break;

                case 1:
                    minX = partX;
                    maxX = threadedAlphaMapWidth;
                    minY = partY;
                    maxY = threadedAlphaMapHeight;
                    break;

                case 2:
                    minX = 0;
                    maxX = partX;
                    minY = partY;
                    maxY = threadedAlphaMapHeight;
                    break;

                case 3:
                    minX = partX;
                    maxX = threadedAlphaMapWidth;
                    minY = partY;
                    maxY = threadedAlphaMapHeight;
                    break;
            }
        }
        #endregion

        #region 8 Threads
        else if (maxWorkerThreads == 8)
        {
            switch (thread)
            {
                case 0:
                    minX = 0;
                    maxX = partX;
                    minY = 0;
                    maxY = partY;
                    break;

                case 1:
                    minX = partX;
                    maxX = partX * 2;
                    minY = 0;
                    maxY = partY;
                    break;

                case 2:
                    minX = partX * 2;
                    maxX = partX * 3;
                    minY = 0;
                    maxY = partY;
                    break;

                case 3:
                    minX = partX * 3;
                    maxX = threadedAlphaMapWidth;
                    minY = 0;
                    maxY = partY;
                    break;

                case 4:
                    minX = 0;
                    maxX = partX;
                    minY = partY;
                    maxY = threadedAlphaMapHeight;
                    break;

                case 5:
                    minX = partX;
                    maxX = partX * 2;
                    minY = partY;
                    maxY = threadedAlphaMapHeight;
                    break;

                case 6:
                    minX = partX * 2;
                    maxX = partX * 3;
                    minY = partY;
                    maxY = threadedAlphaMapHeight;
                    break;

                case 7:
                    minX = partX * 3;
                    maxX = threadedAlphaMapWidth;
                    minY = partY;
                    maxY = threadedAlphaMapHeight;
                    break;
            }
        }


        #endregion

        #region 16 Threads
        else if (maxWorkerThreads == 16)
        {
            switch (thread)
            {
                #region 1st Row

                case 0:
                    minX = 0;
                    maxX = partX;
                    minY = 0;
                    maxY = partY;
                    break;

                case 1:
                    minX = partX;
                    maxX = partX * 2;
                    minY = 0;
                    maxY = partY;
                    break;

                case 2:
                    minX = partX * 2;
                    maxX = partX * 3;
                    minY = 0;
                    maxY = partY;
                    break;

                case 3:
                    minX = partX * 3;
                    maxX = threadedAlphaMapWidth;
                    minY = 0;
                    maxY = partY;
                    break;

                #endregion

                #region 2nd Row

                case 4:
                    minX = 0;
                    maxX = partX;
                    minY = partY;
                    maxY = partY * 2;
                    break;

                case 5:
                    minX = partX;
                    maxX = partX * 2;
                    minY = partY;
                    maxY = partY * 2;
                    break;

                case 6:
                    minX = partX * 2;
                    maxX = partX * 3;
                    minY = partY;
                    maxY = partY * 2;
                    break;

                case 7:
                    minX = partX * 3;
                    maxX = threadedAlphaMapWidth;
                    minY = partY;
                    maxY = partY * 2;
                    break;

                #endregion

                #region 3rd Row

                case 8:
                    minX = 0;
                    maxX = partX;
                    minY = partY * 2;
                    maxY = partY * 3;
                    break;

                case 9:
                    minX = partX;
                    maxX = partX * 2;
                    minY = partY * 2;
                    maxY = partY * 3;
                    break;

                case 10:
                    minX = partX * 2;
                    maxX = partX * 3;
                    minY = partY * 2;
                    maxY = partY * 3;
                    break;

                case 11:
                    minX = partX * 3;
                    maxX = threadedAlphaMapWidth;
                    minY = partY * 2;
                    maxY = partY * 3;
                    break;

                #endregion

                #region 4rd Row

                case 12:
                    minX = 0;
                    maxX = partX;
                    minY = partY * 3;
                    maxY = threadedAlphaMapHeight;
                    break;

                case 13:
                    minX = partX;
                    maxX = partX * 2;
                    minY = partY * 3;
                    maxY = threadedAlphaMapHeight;
                    break;

                case 14:
                    minX = partX * 2;
                    maxX = partX * 3;
                    minY = partY * 3;
                    maxY = threadedAlphaMapHeight;
                    break;

                case 15:
                    minX = partX * 3;
                    maxX = threadedAlphaMapWidth;
                    minY = partY * 3;
                    maxY = threadedAlphaMapHeight;
                    break;

                    #endregion
            }
        }
        #endregion
    }

    #endregion
}