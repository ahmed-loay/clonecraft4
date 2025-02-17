using System.Collections.Generic;
using UnityEngine;
/*
* function "use cycle"
*  on chunk spawn: 
*          //populate Chunk Data array with terrain data => Re-Add voxel verts, tris, etc => create mesh object and apply it
*          generateChunkData => buildMeshData => updateMesh
*  on building a voxel:
*       // modify chunk data array => Add voxel verts, tris, etc => create mesh object and apply it
*       chunkData[x,y,z]=val => buildMeshData => updateMesh
*/

//networking thread receives chunk data, modifies chunkDictionary, adds chunk to queue, main thread updates mesh and applies it
//when a voxel is modified, the chunkdata is modified, added to queue, and a thread sends a voxel modification flag to server

public class Chunk
{
    public MeshFilter filter;
    public MeshRenderer renderer;
    public MeshCollider meshCollider;
    public World world;

    GameObject chunkGO;
    Vector2 pos;

    public byte[,,] chunkData = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    List<int> tris = new List<int>();
    List<Vector3> verts = new List<Vector3>();
    List<Vector2> uvs = new List<Vector2>();
    int vertexIndex = 0;

    //generateChunkData();
    //buildMeshData();
    //updateMesh();

    public Chunk(Vector2 chunkCoord, World _world)
    {
        pos = chunkCoord;
        world = _world;
    }

    public Chunk(Vector2 chunkCoord, World _world, byte[,,] data)
    {
        pos = chunkCoord;
        world = _world;
        chunkData = data;
    }

    //Split gameobject creation from object constructor because another thread will init the object, not the main thread
    public void init()
    {
        chunkGO = new GameObject("Chunk "+ pos.x + ", " + pos.y);
        chunkGO.transform.position = new Vector3(pos.x * VoxelData.chunkWidth, 0f, pos.y * VoxelData.chunkWidth);

        renderer = chunkGO.AddComponent<MeshRenderer>();
        filter = chunkGO.AddComponent<MeshFilter>();

        renderer.material.mainTexture = world.textureAtlas;

        chunkGO.transform.SetParent(world.transform);

        updateMesh();
    }

    //for testing
    public void generateChunkData()
    {
        for (int x = 0; x < VoxelData.chunkWidth; x++)
        {
            int randomHeight = Random.Range(50, 100);
            for (int y = 0; y < VoxelData.chunkHeight; y++)
            {
                if(y < 128)
                    for (int z = 0; z < VoxelData.chunkWidth; z++)
                    {
                        chunkData[x, y, z] = 1; //1 is stone
                    }
            }
        }
    }
    
    //Returns true if solid
    bool checkVoxel(Vector3 pos)
    {
        int x = (int) pos.x;
        int y = (int) pos.y;
        int z = (int) pos.z;

        if (x < 0 || x > VoxelData.chunkWidth - 1 || y < 0 || y > VoxelData.chunkHeight - 1 || z < 0 || z > VoxelData.chunkWidth - 1)
            return false;

        byte block = chunkData[x, y, z];
        return world.blockList[block].isSolid;
    }

    public void setVoxel(Vector3 pos, byte blockType)
    {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        chunkData[(int)pos.x, (int)pos.y, (int)pos.z] = blockType;
        buildMeshData();
        updateMesh();
        watch.Stop();
        Debug.Log("setVoxel: "+watch.ElapsedMilliseconds);
    }

    public void buildMeshData()
    {
        verts.Clear();
        tris.Clear();
        uvs.Clear();

        vertexIndex = 0;

        for (int x = 0; x < VoxelData.chunkWidth; x++)
        {
            for (int y = 0; y < VoxelData.chunkHeight; y++)
            {
                for (int z = 0; z < VoxelData.chunkWidth; z++)
                {
                    //if(chunkData[x, y, z] != 0) //0 is reserved for air
                    if (world.blockList[chunkData[x, y, z]].isSolid) 
                        addVoxelData(new Vector3(x, y, z), chunkData[x, y, z]); //blockType 1 is stone, blocks are in order in jsonarray in the blockconfig file
                    //Debug.Log(chunkData[x, y, z]);
                }
            }
        }
    }

    void addVoxelData(Vector3 pos, byte blockType)
    {
        for (int j = 0; j < 6; j++)
        {
            //checkVoxel function returns true if solid
            if (!checkVoxel(VoxelData.faceChecks[j] + pos))
            {
                verts.Add(VoxelData.verts[VoxelData.tris[j, 0]] + pos);
                verts.Add(VoxelData.verts[VoxelData.tris[j, 1]] + pos);
                verts.Add(VoxelData.verts[VoxelData.tris[j, 2]] + pos);
                verts.Add(VoxelData.verts[VoxelData.tris[j, 3]] + pos);

                /*
                uvs.Add(VoxelData.uvs[0]);
                uvs.Add(VoxelData.uvs[1]);
                uvs.Add(VoxelData.uvs[2]);
                uvs.Add(VoxelData.uvs[3]);
                */

                //From 2 index byte array to Vec2
                byte[] rawXY = world.blockList[blockType].getTexture((byte) j);
                Vector2 textureCoord = new Vector2(rawXY[0], rawXY[1]);

                addTexture(textureCoord);

                tris.Add(vertexIndex);
                tris.Add(vertexIndex + 1);
                tris.Add(vertexIndex + 2);
                tris.Add(vertexIndex + 2);
                tris.Add(vertexIndex + 1);
                tris.Add(vertexIndex + 3);

                vertexIndex += 4;

                /*
                for (int i = 0; i < 6; i++)
                {
                    int currVertex = VoxelData.tris[j, i];
                    verts.Add(VoxelData.verts[currVertex] + pos);
                    uvs.Add(VoxelData.uvs[i]);
                    tris.Add(vertexIndex);
                    vertexIndex++;
                }
                */
            }
        }
    }

    void addTexture(Vector2 blockTexturePosition)
    {
        Vector2 blockBaseTextureCord = new Vector2(blockTexturePosition.x * VoxelData.normalizedBlockSizeX, blockTexturePosition.y * VoxelData.normalizedBlockSizeY);
        Vector2 offset = new Vector2(0.01f, 0.01f);

        uvs.Add(blockBaseTextureCord);
        uvs.Add(blockBaseTextureCord + new Vector2(0, VoxelData.normalizedBlockSizeY));
        uvs.Add(blockBaseTextureCord + new Vector2(VoxelData.normalizedBlockSizeX, 0));
        uvs.Add(blockBaseTextureCord + new Vector2(VoxelData.normalizedBlockSizeX, VoxelData.normalizedBlockSizeY));
    }

    public void updateMesh()
    {
        //var watch = new System.Diagnostics.Stopwatch();
        //watch.Start();


        Mesh generatedMesh = new Mesh();
        generatedMesh.vertices = verts.ToArray();
        generatedMesh.triangles = tris.ToArray();
        generatedMesh.uv = uvs.ToArray();
        generatedMesh.RecalculateNormals();
        //watch.Stop();
        //Debug.Log("mesh object creation: "+watch.ElapsedMilliseconds);

        //watch.Reset();

        //watch.Start();
        filter.mesh = generatedMesh;
        //meshCollider.sharedMesh = generatedMesh;
        //watch.Stop();
        //Debug.Log("settings mesh " + watch.ElapsedMilliseconds);
    }

}
