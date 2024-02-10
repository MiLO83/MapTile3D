using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMap
{
    public int ScreenX;
    public int ScreenY;
    public int[] TileInt; 
    public Color[] TileColors;
    public string MD5Sum;
    public Texture2D TileTexture;
    public GameObject Model;

    public BGMap(int screenx, int screeny, int[] tileInt, Color[] tileColors, string md5Sum, Texture2D tileTexture, GameObject model)
    {
        ScreenX = screenx;
        ScreenY = screeny;
        TileInt = tileInt;
        TileColors = tileColors;
        MD5Sum = md5Sum;
        TileTexture = tileTexture;
        Model = model;
    }
}

