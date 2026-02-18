using System;
using UnityEngine;

[Serializable]
public class BoolMatrix
{
    public int width;
    public int height;

    public bool[] cells;

    public void Resize(int w, int h)
    {
        width = w;
        height = h;
        cells = new bool[w * h];
    }

    public bool Get(int x, int y)
    {
        return cells[y * width + x];
    }

    public void Set(int x, int y, bool value)
    {
        cells[y * width + x] = value;
    }
}