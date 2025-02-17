using UnityEngine;

public static class VoxelData
{
    public static readonly byte chunkWidth = 10;
    public static readonly byte chunkHeight = 128;

    public static float normalizedBlockSizeX;
    public static float normalizedBlockSizeY;
    public static float textureAtlasWidthInBlocks;
    public static float textureAtlasHeightInBlocks;

    public static readonly Vector3[] faceChecks = new Vector3[]
    {
        new Vector3(0, 1, 0),
        new Vector3(0, -1, 0),
        new Vector3(1, 0, 0),
        new Vector3(-1, 0, 0),
        new Vector3(0, 0, -1),
        new Vector3(0, 0, 1),
    };

    public static readonly Vector3[] verts = new Vector3[]
    {
        new Vector3(0, 0, 0),
        new Vector3(0, 0, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 0, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 1, 1),
        new Vector3(1, 1, 1),
        new Vector3(1, 1, 0)
    };

    public static readonly int[,] tris = new int[,]
    {   
        // 4 5 7 7 5 6
        {4, 5, 7, 6}, //top

        {3, 2, 0, 1}, //bottom
        {3, 7, 2, 6}, //right
        {1, 5, 0, 4}, //left
        {0, 4, 3, 7}, //back
        {2, 6, 1, 5}  //front
    };

    public static readonly Vector2[] uvs = new Vector2[]
    {
        new Vector2(0, 0),
        new Vector2(0, 1),
        new Vector2(1, 0),
        new Vector2(1, 1)
    };

}
