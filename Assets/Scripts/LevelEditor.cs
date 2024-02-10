// MapTile3D - By Myles Johnston

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
public class LevelEditor : MonoBehaviour
{
    public Camera tileCamera;
    public Text DebugText;
    public int tileSize = 8;
    public bool exportLevelToOBJ = false;
    public bool exportLevelToIni = true;
    public bool exportTilesToOBJ = true;
    public bool splitOBJByInstance = false;
    public bool convertOneTestImage = false;
    public GameObject TilePrefab;
    public int xAdj = 0;
    public int yAdj = 0;
    public int maxCount = 0;
    public Texture2D MapPNGToSplit;
    public Texture2D MapPNGToSplitHD;
    public Texture2D MapSegmentationMap;
    public Color[] MapSegmentationMapAsColorArray;
    public List<string> segString = new List<string>();
    //public Dictionary<Vector2, Texture2D> SplitMapChunks = new List<Vector2, Texture2D>();
    public Color shouldBeClear;
    public Dictionary<Color[], Texture2D> textureCache = new Dictionary<Color[], Texture2D>();
    public Dictionary<Color[], int> textureCacheNum = new Dictionary<Color[], int>();
    public Dictionary<int, string> map2GameIndex = new Dictionary<int, string>();
    public Dictionary<int, Texture2D> textureCacheByNum = new Dictionary<int, Texture2D>();
    public Dictionary<int, Color[]> colorCacheByNum = new Dictionary<int, Color[]>();
    public Dictionary<int, Color[]> colorCacheByNumOrig = new Dictionary<int, Color[]>();
    public Dictionary<int, Material> materialCacheByNum = new Dictionary<int, Material>();
    public bool[,] found;
    public List<string> mapOutput = new List<string>();
    public static string outputPath = "";
    public List<string> fileNames = new List<string>();
    public List<string> tileFileNames = new List<string>();
    public int count = 0;
    // Start is called before the first frame update
    void Start()
    {
        
        Process();
    }

    public static void SaveTextureAsPNG(Texture2D _texture, string fileName)
    {
        byte[] _bytes = _texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/../MapData/Maps/" + fileName + ".png", _bytes);
        /*
        string prompt = "Video Game Texture";
        Debug.Log("Sending prompt " + prompt);

        StartCoroutine(
            imageAI.GetImage(prompt, (Texture2D texture) =>
            {
                Debug.Log("Done.");
                Renderer renderer = testCube.GetComponent<Renderer>();
                renderer.material.mainTexture = texture;
            },
            useCache: false,
            width: 64, height: 64, steps = 50, promptStrength = 7, seed = -1, image = _bytes
        ));
        */
        System.IO.File.WriteAllBytes(Application.dataPath + "/../MapData/Maps/DO_NOT_MODIFY/" + fileName + ".png", _bytes);
        //Debug.Log(_bytes.Length / 1024 + "Kb was saved as : " + Application.dataPath + "/../MapData/" + fileName + ".png");
    }
    public static void SaveTextureAsPNG_HD(Texture2D _texture, string fileName)
    {
        byte[] _bytes = _texture.EncodeToPNG();
        if (!Directory.Exists(Application.dataPath + "/../MapData/Maps/" + outputPath.Replace(".png", "").Replace("DO_NOT_MODIFY", "") + "_HD"))
        {
            Directory.CreateDirectory(Application.dataPath + "/../MapData/Maps/" + outputPath.Replace(".png", "").Replace("DO_NOT_MODIFY", "") + "_HD");
            Debug.Log("Created Directory : " + (Application.dataPath + "/../MapData/Maps/" + outputPath.Replace(".png", "").Replace("DO_NOT_MODIFY", "") + "_HD"));
        }
        System.IO.File.WriteAllBytes(Application.dataPath + "/../MapData/Maps/" + outputPath.Replace(".png", "").Replace("DO_NOT_MODIFY", "") + "_HD/" + fileName + ".png", _bytes);
        Debug.Log("Wrote : " + (Application.dataPath + "/../MapData/Maps/" + outputPath.Replace(".png", "").Replace("DO_NOT_MODIFY", "") + "_HD/" + fileName + ".png"));
        /*
        string prompt = "Video Game Texture";
        Debug.Log("Sending prompt " + prompt);

        StartCoroutine(
            imageAI.GetImage(prompt, (Texture2D texture) =>
            {
                Debug.Log("Done.");
                Renderer renderer = testCube.GetComponent<Renderer>();
                renderer.material.mainTexture = texture;
            },
            useCache: false,
            width: 64, height: 64, steps = 50, promptStrength = 7, seed = -1, image = _bytes
        ));
        */
        //Debug.Log(_bytes.Length / 1024 + "Kb was saved as : " + Application.dataPath + "/../MapData/" + fileName + ".png");
    }

    public async void Process()
    {
        //ImageAI imageAI = Misc.GetAddComponent<ImageAI>(gameObject);
        if (!Directory.Exists(Application.dataPath + "/../MapData/"))
        {
            Directory.CreateDirectory(Application.dataPath + "/../MapData/");
        }
        if (!Directory.Exists(Application.dataPath + "/../MapData/Maps/"))
        {
            Directory.CreateDirectory(Application.dataPath + "/../MapData/Maps/");
        }
        if (!Directory.Exists(Application.dataPath + "/../MapData/Maps/DO_NOT_MODIFY/"))
        {
            Directory.CreateDirectory(Application.dataPath + "/../MapData/Maps/DO_NOT_MODIFY/");
        }
        if (!Directory.Exists(Application.dataPath + "/../MapData/Input/"))
        {
            Directory.CreateDirectory(Application.dataPath + "/../MapData/Input/");
        }
        DirectoryInfo di = new DirectoryInfo(Application.dataPath + "/../MapData/Input/");
        if (!convertOneTestImage)
        {
            foreach (FileInfo fi in di.GetFiles("*.png", SearchOption.AllDirectories))
            {
                fileNames.Add(fi.FullName);
            }
        }
        else
        {
            fileNames.Add(di.GetFiles("*.png", SearchOption.AllDirectories)[0].FullName);
        }
        foreach (string fileName in fileNames) {
            for (int pass = 0; pass < 2; pass++)
            {
                DirectoryInfo tdi = new DirectoryInfo(Application.dataPath + "/../MapData/Maps/DO_NOT_MODIFY/");
                int count = 0;
                foreach (FileInfo tfi in tdi.GetFiles("*.png", SearchOption.AllDirectories))
                {
                    if (!tfi.Name.Contains("_alpha") && !tfi.Name.Contains("_"))
                    {
                        count = 987654321;
                        int.TryParse(tfi.Name.Substring(0, tfi.Name.IndexOf(".")).Replace("$", ""), out count);
                        if (count != 987654321)
                        {
                            if (!colorCacheByNum.ContainsKey(count))
                            {
                                DebugText.text = "Loading :" + Environment.NewLine + tfi.Name;
                                Texture2D tex = new Texture2D(tileSize, tileSize, TextureFormat.RGBA32, false);
                                byte[] imageBytes = File.ReadAllBytes(tfi.FullName);
                                tex.LoadImage(imageBytes);
                                tex.Apply();
                                tex.filterMode = FilterMode.Point;
                                tex.wrapMode = TextureWrapMode.Clamp;
                                Color[] tileColors = new Color[tileSize * tileSize];
                                tileColors = tex.GetPixels();
                                textureCache.Add(tileColors, tex);
                                textureCacheByNum.Add(count, tex);
                                textureCacheNum.Add(tileColors, count);
                                colorCacheByNum.Add(count, tileColors);
                                Material mat = new Material(Shader.Find("Unlit/Transparent"));
                                mat.name = count.ToString();
                                mat.mainTexture = tex;
                                materialCacheByNum.Add(count, mat);
                                if (count > maxCount)
                                    maxCount = count;
                                //count++;
                                Debug.Log("colorCacheByNum.Count = " + colorCacheByNum.Count);
                            }
                        }
                    }
                }
                DirectoryInfo tdio = new DirectoryInfo(Application.dataPath + "/../MapData/Maps/DO_NOT_MODIFY/");
                string nesTile = "?";
                foreach (FileInfo tfio in tdio.GetFiles("*.png", SearchOption.AllDirectories))
                {
                    if (!tfio.Name.Contains("_alpha") && tfio.Name.Contains("_"))
                    {
                        Debug.Log(tfio.Name);
                        nesTile = tfio.Name.Replace(".png","");
                        DebugText.text = "Loading :" + Environment.NewLine + tfio.Name + " Count = " + nesTile;
                        Texture2D tex = new Texture2D(tileSize, tileSize, TextureFormat.RGBA32, false);
                        byte[] imageBytes = File.ReadAllBytes(tfio.FullName);
                        tex.LoadImage(imageBytes);
                        tex.Apply();
                        tex.filterMode = FilterMode.Point;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        Color[] tileColors = new Color[tileSize * tileSize];
                        tileColors = tex.GetPixels();
                        int matchCount = 0;
                        int mostMatches = 0;
                        float minRDiff = float.MaxValue;
                        float minGDiff = float.MaxValue;
                        float minBDiff = float.MaxValue;
                        for (int i = 0; i < colorCacheByNum.Count; i++)
                        {
                            matchCount = 0;

                            for (int stampy = 0; stampy < tileSize; stampy++)
                            {
                                for (int stampx = 0; stampx < tileSize; stampx++)
                                {
                                    /*
                                    if (tileColors[stampx + (stampy * tileSize)].a == colorCacheByNum[i][stampx + (stampy * tileSize)].a)
                                    {
                                        matchCount++;
                                    }
                                    else*/
                                    {
                                        if (Mathf.Abs(tileColors[stampx + (stampy * tileSize)].r - colorCacheByNum[i][stampx + (stampy * tileSize)].r) <= minRDiff && Mathf.Abs(tileColors[stampx + (stampy * tileSize)].g - colorCacheByNum[i][stampx + (stampy * tileSize)].g) <= minGDiff && Mathf.Abs(tileColors[stampx + (stampy * tileSize)].b - colorCacheByNum[i][stampx + (stampy * tileSize)].b) <= minBDiff)
                                        {
                                            minRDiff = Mathf.Abs(tileColors[stampx + (stampy * tileSize)].r - colorCacheByNum[i][stampx + (stampy * tileSize)].r);
                                            minGDiff = Mathf.Abs(tileColors[stampx + (stampy * tileSize)].g - colorCacheByNum[i][stampx + (stampy * tileSize)].g);
                                            minBDiff = Mathf.Abs(tileColors[stampx + (stampy * tileSize)].b - colorCacheByNum[i][stampx + (stampy * tileSize)].b);
                                            matchCount++;
                                            mostMatches = i;
                                        }
                                    }

                                }
                            }


                        }
                        if (!map2GameIndex.ContainsKey(mostMatches))
                        {
                            map2GameIndex.Add(mostMatches, nesTile);
                            Debug.Log(nesTile);
                        }
                        /*
                        textureCache.Add(tileColors, tex);
                        textureCacheByNum.Add(count, tex);
                        textureCacheNum.Add(tileColors, count);
                        colorCacheByNum.Add(count, tileColors); 
                        Material mat = new Material(Shader.Find("Unlit/Transparent"));
                        mat.name = ocount.ToString();
                        mat.mainTexture = tex;
                        materialCacheByNum.Add(count, mat);
                        if (ocount > maxCount)
                            maxCount = ocount;
                        ocount++;
                        */
                    }
                }


                {
                    if (!fileName.Contains("_HD"))
                    {
                        DebugText.text = "";
                        byte[] imageBytes = File.ReadAllBytes(fileName);
                        FileInfo fi = new FileInfo(fileName);
                        outputPath = fi.Name.Split('.')[0];
                        MapPNGToSplit = new Texture2D(2, 2);
                        MapPNGToSplit.LoadImage(imageBytes);
                        MapPNGToSplit.Apply();
                        MapPNGToSplit.filterMode = FilterMode.Point;
                        bool hasHD = false;
                        if (File.Exists(Application.dataPath + "/../MapData/Input/" + outputPath + "_HD.png"))
                        {
                            byte[] imageBytesHD = File.ReadAllBytes(Application.dataPath + "/../MapData/Input/" + outputPath + "_HD.png");
                            MapPNGToSplitHD = new Texture2D(2, 2);
                            MapPNGToSplitHD.LoadImage(imageBytesHD);
                            MapPNGToSplitHD.Apply();
                            MapPNGToSplitHD.filterMode = FilterMode.Point;
                            hasHD = true;
                            Debug.Log("Has HD Map!");
                        }
                        MapSegmentationMap = new Texture2D(MapPNGToSplit.width, MapPNGToSplit.height);
                        MapSegmentationMapAsColorArray = new Color[MapPNGToSplit.width * MapPNGToSplit.height];
                        found = new bool[(int)(MapPNGToSplit.width), ((int)(MapPNGToSplit.height))];
                        shouldBeClear = MapPNGToSplit.GetPixel(MapPNGToSplit.width - 1, MapPNGToSplit.height - 1);
                        Camera.main.backgroundColor = shouldBeClear;
                        mapOutput.Clear();

                        GameObject[] levelObjects = GameObject.FindGameObjectsWithTag("Level");
                        for (var i = 0; i < levelObjects.Length; i++)
                        {
                            Destroy(levelObjects[i]);
                        }

                        for (int y = 0; y < (int)(MapPNGToSplit.height); y += tileSize)
                        {
                            string mapLine = "";
                            for (int x = 0; x < (int)(MapPNGToSplit.width); x += tileSize)
                            {
                                found[(int)(x / tileSize), (int)(y / tileSize)] = false;
                                Color32 segColor = new Color32(0, 0, 0, 255);
                                int foundLocation = -1;
                                int R = 0;
                                int G = 0;
                                int B = 0;
                                if (found[x, y] == false)
                                {
                                    Color[] tileColors = new Color[tileSize * tileSize];
                                    Color[] tileColorsAlpha = new Color[tileSize * tileSize];
                                    Color[] tileColorsHD = new Color[tileSize * tileSize];
                                    for (int stampy = 0; stampy < tileSize; stampy++)
                                    {
                                        for (int stampx = 0; stampx < tileSize; stampx++)
                                        {
                                            tileColors[stampx + ((stampy) * tileSize)] = MapPNGToSplit.GetPixel(((x)) + (int)(stampx), ((y)) + (int)(stampy));
                                            if (hasHD)
                                                tileColorsHD[stampx + ((stampy) * tileSize)] = MapPNGToSplitHD.GetPixel(((x)) + (int)(stampx), ((y)) + (int)(stampy));

                                            R += (int)(tileColors[stampx + ((stampy) * tileSize)].r * 255f);
                                            G += (int)(tileColors[stampx + ((stampy) * tileSize)].g * 255f);
                                            B += (int)(tileColors[stampx + ((stampy) * tileSize)].b * 255f);
                                            tileColorsAlpha[stampx + ((stampy) * tileSize)] = Color.white;
                                            if (tileColors[stampx + ((stampy) * tileSize)].Equals(shouldBeClear))
                                            {
                                                tileColorsAlpha[stampx + ((stampy) * tileSize)] = Color.black;
                                            }
                                        }
                                    }
                                    R /= tileSize * tileSize;
                                    G /= tileSize * tileSize;
                                    B /= tileSize * tileSize;
                                    for (int t = 0; t < colorCacheByNum.Count; t++)
                                    {
                                        bool isEqual = Enumerable.SequenceEqual(colorCacheByNum[t], tileColors);
                                        if (isEqual)
                                        {
                                            foundLocation = colorCacheByNum.ElementAt(t).Key;
                                        }
                                    }
                                    segColor = new Color32((byte)R, (byte)G, (byte)B, 255);

                                    for (int stampy = 0; stampy < tileSize; stampy++)
                                    {
                                        for (int stampx = 0; stampx < tileSize; stampx++)
                                        {
                                            if (tileColorsAlpha[stampx + ((stampy) * tileSize)] == Color.white)
                                            {
                                                MapSegmentationMapAsColorArray[(x + stampx) + ((y + stampy) * MapSegmentationMap.width)] = segColor;
                                            }
                                            else
                                            {
                                                MapSegmentationMapAsColorArray[(x + stampx) + ((y + stampy) * MapSegmentationMap.width)] = Color.cyan;
                                            }
                                        }
                                    }
                                    if (foundLocation != -1)
                                    {
                                        GameObject tile = GameObject.Instantiate(TilePrefab);
                                        if (!splitOBJByInstance)
                                        {
                                            tile.name = foundLocation.ToString("X2");
                                        }
                                        else
                                        {
                                            tile.name = "T" + foundLocation.ToString("X2") + "X" + (int)(x / tileSize) + "Y" + (int)(y / tileSize);
                                        }
                                        // Debug.Log("Tile # " + tile.name + " found @ X : " + x + ", Y : " + y);
                                        tile.transform.position = new Vector3((int)(x / tileSize) + 0.5f, (int)(y / tileSize) + 0.5f, 0);
                                        tile.GetComponent<Renderer>().material = materialCacheByNum[foundLocation];
                                        tile.GetComponent<Renderer>().material.name = foundLocation.ToString().PadLeft(3, '0');
                                        tile.GetComponent<Renderer>().material.mainTexture = textureCacheByNum[foundLocation];
                                        tile.tag = "Level";
                                        //Debug.Log("Tile # " + tile.name + " found @ X : " + x + ", Y : " + y);
                                        tile.transform.position = new Vector3((int)(x / tileSize) + 0.5f, (int)(y / tileSize) + 0.5f, 0);
                                        tile.transform.parent = gameObject.transform;
                                        found[(int)(x / tileSize), (int)(y / tileSize)] = true;
                                        if (map2GameIndex.ContainsKey(foundLocation))
                                        {
                                            mapLine += map2GameIndex[foundLocation] + ",";
                                        }
                                        else
                                        {
                                            mapLine += foundLocation.ToString("").PadLeft(3, '0') + "$,";
                                        }
                                        if (hasHD)
                                        {
                                            Texture2D texHD = new Texture2D(tileSize, tileSize, TextureFormat.RGBA32, false);
                                            texHD.SetPixels(tileColorsHD);
                                            texHD.Apply();
                                            if (map2GameIndex.ContainsKey(foundLocation))
                                            {
                                                SaveTextureAsPNG_HD(texHD, map2GameIndex[foundLocation] + "_HD-(X)" + (int)(x / tileSize) + "(Y)" + (int)(y / tileSize));
                                            }
                                            else
                                            {
                                                SaveTextureAsPNG_HD(texHD, foundLocation.ToString("") + "$_HD-(X)" + (int)(x / tileSize) + "(Y)" + (int)(y / tileSize));
                                            }
                                        }
                                        Texture2D tex = new Texture2D(tileSize, tileSize, TextureFormat.RGBA32, false);
                                        tex.SetPixels(tileColors);
                                        tex.Apply();
                                        Texture2D texa = new Texture2D(tileSize, tileSize, TextureFormat.RGBA32, false);
                                        texa.SetPixels(tileColorsAlpha);
                                        texa.Apply();
                                        if (map2GameIndex.ContainsKey(foundLocation))
                                        {
                                            SaveTextureAsPNG(tex, map2GameIndex[foundLocation]);
                                            SaveTextureAsPNG(texa, map2GameIndex[foundLocation] + "_alpha");
                                        }
                                        else
                                        {
                                            SaveTextureAsPNG(tex, foundLocation.ToString().PadLeft(3, '0') + "$");
                                            SaveTextureAsPNG(texa, foundLocation.ToString().PadLeft(3, '0') + "$_alpha");
                                        }
                                        tileCamera.transform.position = new Vector3((int)(x / tileSize) + 0.5f, (int)(y / tileSize) - 5f, -10);
                                        await Task.Delay(5);
                                    }
                                    else
                                    {
                                        if (hasHD)
                                        {
                                            Texture2D texHD = new Texture2D(tileSize, tileSize, TextureFormat.RGBA32, false);
                                            texHD.SetPixels(tileColorsHD);
                                            texHD.Apply();
                                            SaveTextureAsPNG_HD(texHD, count.ToString().PadLeft(3, '0') + "$_HD-(X)" + (int)(x / tileSize) + "(Y)" + (int)(y / tileSize));
                                        }

                                        if (!textureCache.ContainsKey(tileColors))
                                        {
                                            Texture2D tex = new Texture2D(tileSize, tileSize, TextureFormat.RGBA32, false);
                                            tex.SetPixels(tileColors);
                                            tex.Apply();
                                            Texture2D texa = new Texture2D(tileSize, tileSize, TextureFormat.RGBA32, false);
                                            texa.SetPixels(tileColorsAlpha);
                                            texa.Apply();
                                            tex.filterMode = FilterMode.Point;
                                            tex.wrapMode = TextureWrapMode.Clamp;
                                            if (!textureCacheByNum.ContainsKey(count))
                                            {
                                                textureCache.Add(tileColors, tex);
                                                textureCacheByNum.Add(count, tex);
                                                textureCacheNum.Add(tileColors, count);
                                                colorCacheByNum.Add(count, tileColors);
                                            }
                                            //Debug.Log(count + " Tiles Scanned!");
                                            GameObject tile = GameObject.Instantiate(TilePrefab);

                                            if (!splitOBJByInstance)
                                            {
                                                tile.name = count.ToString().PadLeft(3, '0');
                                            }
                                            else
                                            {
                                                tile.name = "T" + count.ToString().PadLeft(3, '0') + "X" + (int)(x / tileSize) + "Y" + (int)(y / tileSize);
                                            }
                                            tile.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Transparent"));
                                            tile.GetComponent<Renderer>().material.name = count.ToString().PadLeft(3, '0');
                                            tile.GetComponent<Renderer>().material.mainTexture = tex;
                                            if (exportTilesToOBJ)
                                            {
                                                tile.tag = "Tile";
                                                tile.transform.position = new Vector3(0, 0, 0);
                                                GetComponent<ExportOBJ>().ExportScene(count.ToString().PadLeft(3, '0'), "Tile");
                                            }
                                            tile.tag = "Level";
                                            tile.transform.position = new Vector3((int)(x / tileSize) + 0.5f, (int)(y / tileSize) + 0.5f, 0);
                                            tile.transform.parent = gameObject.transform;
                                            if (!materialCacheByNum.ContainsKey(count))
                                            {
                                                materialCacheByNum.Add(count, tile.GetComponent<Renderer>().material);
                                            }
                                            SaveTextureAsPNG(tex, count.ToString().PadLeft(3, '0') + "$");
                                            SaveTextureAsPNG(texa, count.ToString().PadLeft(3, '0') + "$_alpha");
                                            found[(int)(x / tileSize), (int)(y / tileSize)] = true;
                                            mapLine += count.ToString().PadLeft(3, '0') + "$,";
                                            tileCamera.transform.position = new Vector3((int)(x / tileSize) + 0.5f, (int)(y / tileSize) - 5f, -10);
                                            await Task.Delay(5);
                                            count++;
                                        }
                                    }
                                    //Debug.Log("SECOND PASS : X = " + x + " , Y = " + y);



                                }
                                if (!segString.Contains("(" + R + "," + G + "," + B + "): " + '"' + foundLocation + ":1.0" + '"' + ","))
                                {
                                    segString.Add("(" + R + "," + G + "," + B + "): " + '"' + foundLocation + ":1.0" + '"' + ",");
                                }


                                

                            }
                            mapOutput.Insert(0, mapLine);
                        }
                        DebugText.text = "DONE GENERATING MAP!";
                        await Task.Delay(3000);
                        if (exportLevelToIni)
                        {
                            if (pass == 0)
                            {
                                File.WriteAllLines(Application.dataPath + "/../MapData/Maps/" + outputPath + ".ini", mapOutput.ToArray());
                            } else
                            {
                                File.WriteAllLines(Application.dataPath + "/../MapData/Maps/" + outputPath + "_NES.ini", mapOutput.ToArray());
                            }
                            DebugText.text = "DONE WRITING MAP .INI!";
                            await Task.Delay(3000);
                        }
                        if (exportLevelToOBJ)
                        {
                            if (pass == 0)
                            {
                                GetComponent<ExportOBJ>().ExportScene(outputPath, "Level");
                            } else
                            {
                                GetComponent<ExportOBJ>().ExportScene(outputPath + "_NES", "Level");
                            }
                            DebugText.text = "DONE WRITING MAP .OBJ!";
                            await Task.Delay(3000);
                        }
                        MapSegmentationMap.SetPixels(MapSegmentationMapAsColorArray);
                        MapSegmentationMap.Apply();
                        if (!Directory.Exists(Application.dataPath + "/../MapData/Maps/" + outputPath.Replace(".png", "").Replace("DO_NOT_MODIFY", "")))
                        {
                            Directory.CreateDirectory(Application.dataPath + "/../MapData/Maps/" + outputPath.Replace(".png", "").Replace("DO_NOT_MODIFY", ""));
                        }

                        byte[] _bytes = MapSegmentationMap.EncodeToPNG();
                        System.IO.File.WriteAllBytes(Application.dataPath + "/../MapData/Maps/" + outputPath.Replace(".png", "").Replace("DO_NOT_MODIFY", "") + "/SegmentationMap" + ".png", _bytes);


                        File.WriteAllLines(Application.dataPath + "/../MapData/Maps/" + outputPath.Replace(".png", "").Replace("DO_NOT_MODIFY", "") + "/" + "SegmentationKey.txt", segString.ToArray());
                    }
                }
            }
        }
        //Application.Quit();
    }

    Bounds CalculateBounds(GameObject go)
    {
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        Component[] rList = go.GetComponentsInChildren(typeof(Renderer));
        foreach (Renderer r in rList)
        {
            b.Encapsulate(r.bounds);
        }
        return b;
    }
    public void FocusCameraOnGameObject(Camera c, GameObject go)
    {
        Bounds b = CalculateBounds(go);
        Vector3 max = b.size;
        // Get the radius of a sphere circumscribing the bounds
        float radius = max.magnitude / 2f;
        // Get the horizontal FOV, since it may be the limiting of the two FOVs to properly encapsulate the objects
        float horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(c.fieldOfView * Mathf.Deg2Rad / 2f) * c.aspect) * Mathf.Rad2Deg;
        // Use the smaller FOV as it limits what would get cut off by the frustum        
        float fov = Mathf.Min(c.fieldOfView, horizontalFOV);
        float dist = radius / (Mathf.Sin(fov * Mathf.Deg2Rad / 2f));
        //Debug.Log("Radius = " + radius + " dist = " + dist);
        c.transform.localPosition = new Vector3(0, 0, dist);
        if (c.orthographic)
            c.orthographicSize = (radius/2)*1.15f;

        // Frame the object hierarchy
        c.transform.position = b.center;
        c.transform.LookAt(b.center);
    }
}
