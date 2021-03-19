using System;
using System.Collections.Generic;
using System.IO;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Ext.RustMap
{
    public class RustMap : CSPlugin
    {
        Dictionary<string, Color> materialsColors = new Dictionary<string, Color>
        {
            { "Rock (Instance)",    new Color(0.6f, 0.6f, 0.6f) },
            { "Concrete (Instance)",new Color(0.8f, 0.8f, 0.8f) },
            { "Metal (Instance)",   new Color(0.7f, 0.4f, 0.24f) },
            { "Snow (Instance)",    Color.white }
        };

        Vector3 sunrayVector = new Vector3(-0.5f, 1f, -1.5f);
        float density = 1f;
        Color colorWaterMaxLevel = new Color(0.07058824f, 0.203921571f, 0.3372549f);
        int mask = 8716289;
        private float deepVisibleGround = 10f;
        private float deepMax = 10f;
        private float maxShading = 0.7f;

        [HookMethod("OnTerrainInitialized")]
        private void OnTerrainInitialized()
        {
            this.GenerateMap();
        }

        private void GenerateMap()
        {
            string mapName = string.Format("map_{0}_{1}.png", World.Seed, World.Size);
            if (File.Exists(mapName))
                return;

            Debug.LogWarning("Starting map generate");

            /*int[]   mapSize = { 
                (int)(TerrainMeta.Size.x / density), 
                (int)(TerrainMeta.Size.z / density) 
            };*/
            int[] mapSize = { (int)(World.Size / density), (int)(World.Size / density) };
            float[] mapStart = { TerrainMeta.Position.x, TerrainMeta.Position.z };
            TerrainColors colors = TerrainMeta.Colors;
            Texture.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            Texture2D texture2D = new Texture2D(mapSize[0], mapSize[1]);
            texture2D.anisoLevel = 16;
            texture2D.mipMapBias = 0.1f;

            Debug.LogWarningFormat("{0} {1}", mapSize[0], mapSize[1]);
            Debug.LogWarningFormat("{0} {1}", mapStart[0], mapStart[1]);

            Color[,] colorMap = new Color[Mathf.CeilToInt(mapSize[0]), Mathf.CeilToInt(mapSize[1])];
            float[,] heightMap = new float[Mathf.CeilToInt(mapSize[0]), Mathf.CeilToInt(mapSize[1])];
            float[,] depthMap = new float[Mathf.CeilToInt(mapSize[0]), Mathf.CeilToInt(mapSize[1])];
            float[,] shadowMap = new float[Mathf.CeilToInt(mapSize[0]), Mathf.CeilToInt(mapSize[1])];
            Vector3[,] normalMap = new Vector3[Mathf.CeilToInt(mapSize[0]), Mathf.CeilToInt(mapSize[1])];

            Debug.LogWarning("RUSTMAP: Create base color map");

            ulong pointsCount = Convert.ToUInt64(mapSize[0]) * Convert.ToUInt64(mapSize[1]) / 
                                Convert.ToUInt64(density) / Convert.ToUInt64(density);
            ulong processedPointsCount = 0;

            for (float x = 0f; x < mapSize[0] * density; x += density)
                for (float z = 0f; z < mapSize[1] * density; z += density)
                {
                    int xarr = (int)(x / density);
                    int yarr = (int)(z / density);
                    RaycastHit hit;
                    Vector3 pointOnMap = new Vector3(mapStart[0] + x, 0f, mapStart[1] + z);
                    Color pointColor = colors.GetColor(pointOnMap, -1);
                    bool collided = false;
                    if (Physics.BoxCast(pointOnMap + Vector3.up * 1000f, Vector3.one * density, Vector3.down,
                                        out hit, default(Quaternion), 1100f, mask,
                                        QueryTriggerInteraction.Ignore))
                    {
                        collided = true;
                        if (hit.collider != null)
                            if (materialsColors.ContainsKey(hit.collider.material.name))
                                pointColor = materialsColors[hit.collider.material.name];
                    }
                    colorMap[xarr, yarr] = pointColor;
                    heightMap[xarr, yarr] = collided ? hit.point.y : TerrainMeta.HeightMap.GetHeight(pointOnMap);
                    depthMap[xarr, yarr] = TerrainMeta.WaterMap.GetDepth(pointOnMap);

                    if (processedPointsCount % (pointsCount / 100) == 0)
                        Debug.LogWarningFormat("{0}% done", processedPointsCount / (pointsCount / 100));
                    processedPointsCount++;
                }

            Debug.LogWarning("RUSTMAP: Normal map generation");
            for (int x = 0; x < mapSize[0]; x++)
                for (int y = 0; y < mapSize[1]; y++)
                {
                    Vector3 result;
                    if (x == 0 || y == 0 || x == mapSize[0] - 1 || y == mapSize[1] - 1)
                        result = Vector3.up;
                    else
                        result = new Vector3(
                            heightMap[x + 1, y] - heightMap[x - 1, y],
                            1f,
                            heightMap[x, y + 1] - heightMap[x, y - 1]
                        );
                    normalMap[x, y] = result.normalized;
                }

            Debug.LogWarning("RUSTMAP: Shadow map generating");
            for (int x = 0; x < mapSize[0]; x++)
                for (int y = 0; y < mapSize[1]; y++)
                {
                    Vector3 groundVec = normalMap[x, y];
                    float angle = Vector3.Angle(groundVec, sunrayVector);
                    shadowMap[x, y] = Mathf.Lerp(0f, 
                        maxShading, 
                        Mathf.Clamp((angle > 180f) ? (180f - angle) : angle, 0f, 90f) / 90f
                    );
                }

            Debug.LogWarning("RUSTMAP: Map image compiling");
            for (int x = 0; x < mapSize[0]; x++)
                for (int y = 0; y < mapSize[1]; y++)
                {
                    Color color = colorMap[x, y];
                    float depth = depthMap[x, y];
                    float height = heightMap[x, y];
                    float shade = shadowMap[x, y];
                    if (depth > 0f || height < 0)
                    {
                        color = GetWaterColor(color, depth);
                        color *= 1f - maxShading;
                        color.a = 1f;
                    }
                    else
                    {
                        color *= 1f - shade;
                        color.a = 1f;
                    }
                    colorMap[x, y] = color;
                }

            Debug.LogWarning("RUSTMAP: Bluring color map");
            for (int x = 1; x < mapSize[0] - 1; x++)
                for (int y = 1; y < mapSize[1] - 1; y++)
                {
                    Color[] pointsNear = {
                        /*colorMap[x - 1, y - 1],*/ colorMap[x - 1, y], /*colorMap[x - 1, y + 1],*/
                        colorMap[x, y - 1], colorMap[x, y], colorMap[x, y + 1],
                        /*colorMap[x + 1, y - 1],*/ colorMap[x + 1, y], /*colorMap[x + 1, y + 1] */
                    };
                    float r = 0, g = 0, b = 0;
                    foreach (Color c in pointsNear)
                    {
                        r += c.r;
                        g += c.g;
                        b += c.b;
                    }
                    r /= pointsNear.Length;
                    g /= pointsNear.Length;
                    b /= pointsNear.Length;
                    colorMap[x, y] = new Color(r, g, b);
                }

            Debug.LogWarning("RUSTMAP: Save map");
            for (int x = 0; x < mapSize[0]; x++)
                for (int y = 0; y < mapSize[1]; y++)
                    texture2D.SetPixel(x, y, colorMap[x, y]);
            File.WriteAllBytes(mapName, texture2D.EncodeToPNG());
            UnityEngine.Object.Destroy(texture2D);

            /*Texture2D heightTex = new Texture2D(mapSize[0], mapSize[1]);
            heightTex.anisoLevel = 16;
            heightTex.mipMapBias = 0.1f;
            for (int x = 0; x < mapSize[0]; x++)
                for (int y = 0; y < mapSize[0]; y++)
                {
                    Color c = Color.white;
                    float height = heightMap[x, y];
                    if (height < 0f)
                    {
                        float clampedHeight = Mathf.Clamp(Mathf.Abs(height), 0f, 20f);
                        float col = 0.5f + Mathf.Lerp(0f, 0.5f, clampedHeight / 20f);
                        c.r -= col; c.g -= col; c.b -= col;
                    }
                    heightTex.SetPixel(x, y, c);
                }

            File.WriteAllBytes("heightMap.png", heightTex.EncodeToPNG());
            UnityEngine.Object.Destroy(heightTex);*/
        }

        private void DrawMonument(MonumentInfo monument)
        {
            //TODO
        }

        private Color GetWaterColor(Color baseColor, float depth)
        {
            depth = Mathf.Clamp(depth, 0f, deepMax);
            baseColor *= Mathf.Lerp(0.95f, 0.3f, depth / deepVisibleGround);
            //baseColor *= 0.4f - Mathf.Lerp(0f, 0.2f, Mathf.Clamp(depth, 0f, this.deepVisibleGround) / this.deepVisibleGround);
            baseColor.g += Mathf.Lerp(0.1f, 0.3f, Mathf.Clamp(depth, 0f, this.deepVisibleGround) / this.deepVisibleGround);
            baseColor.b += Mathf.Lerp(0.2f, 0.6f, Mathf.Clamp(depth, 0f, this.deepVisibleGround) / this.deepVisibleGround);
            return new Color(
                Mathf.Lerp(baseColor.r, this.colorWaterMaxLevel.r, depth / this.deepVisibleGround), 
                Mathf.Lerp(baseColor.g, this.colorWaterMaxLevel.g, depth / this.deepVisibleGround), 
                Mathf.Lerp(baseColor.b, this.colorWaterMaxLevel.b, depth / this.deepVisibleGround));
        }
    }
}
