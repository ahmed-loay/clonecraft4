using UnityEngine;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
//using NativeWebSocket;
using Newtonsoft.Json;

public class World : MonoBehaviour
{
    public string address;
    public string textureAtlasPath;
    public Texture2D textureAtlas;
    public blockType[] blockList;
    public Chunk chunk;
    TcpClient client = null;

    ConcurrentDictionary<Vector2, Chunk> chunkDictionary = new ConcurrentDictionary<Vector2, Chunk>();
    ConcurrentQueue<Vector2> chunksToBuild = new ConcurrentQueue<Vector2>();

    void Start()
    {
        Debug.Log("Generating Block Types Array..");
        string json = File.ReadAllText("blockTextures.config");
        blockList = JsonConvert.DeserializeObject<List<blockType>>(json).ToArray();

        
        Debug.Log("Loading Texture Atlas..");
        textureAtlas = new Texture2D(1, 1);
        ImageConversion.LoadImage(textureAtlas, File.ReadAllBytes(textureAtlasPath));

        textureAtlas.filterMode = FilterMode.Point;
        textureAtlas.wrapMode = TextureWrapMode.Clamp;

        VoxelData.textureAtlasWidthInBlocks = textureAtlas.width / 16; //128 is the default texture size for each block, here its 16 because im using some other texture atlas
        VoxelData.textureAtlasHeightInBlocks = textureAtlas.height / 16;

        VoxelData.normalizedBlockSizeX = 1 / VoxelData.textureAtlasWidthInBlocks;
        VoxelData.normalizedBlockSizeY = 1 / VoxelData.textureAtlasHeightInBlocks;
        /*
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        for (int x = 0; x <= 5; x++)
        {
            for (int y = 0; y <= 5; y++)
            {
                Chunk c = new Chunk(new Vector2(x, y), this); //in thread
                c.generateChunkData(); //taken from network in thread anyways
                c.chunkData[0, 0, 0] = (byte) blockType.BlockType.WoodenPlanks;
                c.buildMeshData(); //in thread too, but a seperate one from the networking one
                c.init();//in main thread, by queuing
            }
        }

        watch.Stop();
        Debug.Log(watch.ElapsedMilliseconds + "ms");
        */
        /*
        Chunk c = new Chunk(new Vector2(0, 0), this); //in thread
        c.generateChunkData(); //taken from network in thread anyways
        c.buildMeshData(); //in thread too, but a seperate one from the networking one
        c.init();//in main thread, by queuing
        */

        Debug.Log("Connecting To Server..");
        ThreadPool.QueueUserWorkItem(new WaitCallback(networkingThread));
    }

    private void Update()
    {
        if (chunksToBuild.Count != 0)
            StartCoroutine(chunkBuild());
    }

    IEnumerator chunkBuild()
    {
        while (chunksToBuild.Count != 0)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            Vector2 chunkPos;
            chunksToBuild.TryDequeue(out chunkPos);

            Chunk chunkObject;
            chunkDictionary.TryGetValue(chunkPos, out chunkObject);
            chunkObject.init();
            chunkObject.updateMesh();

            watch.Stop();
            //Debug.Log("create chunk gameobject and applying mesh took " + watch.ElapsedMilliseconds + "ms");
            yield return null;
        }
    }

    private void OnApplicationQuit()
    {
        NetworkStream ns = client.GetStream();
        BinaryWriter writer = new BinaryWriter(ns);
        writer.Write(1);//packet size
        writer.Write(1);//opcode (1 for disconnect)
        writer.Flush();
    }

    void networkingThread(object state)
    {
        client = new TcpClient();
        client.Connect("localhost", 9420);

        NetworkStream ns = client.GetStream();

        BinaryWriter writer = new BinaryWriter(ns);
        BinaryReader reader = new BinaryReader(ns);

        byte worldSize = 50;

        for (int x = 0; x <= worldSize; x++)
        {
            for (int y = 0; y <= worldSize; y++)
            {
                List<byte> packetToSend = new List<byte>();
                packetToSend.Add(0); //opcode (opcode size is only 1 byte)
                groupAdd(ref packetToSend, BitConverter.GetBytes(x)); //x, y, z
                groupAdd(ref packetToSend, BitConverter.GetBytes(0)); //y is redundant for now
                groupAdd(ref packetToSend, BitConverter.GetBytes(y));

                writer.Write(packetToSend.Count); //write length first
                writer.Write(packetToSend.ToArray()); //write actual packet that we sent its length of
            }
        }
        writer.Flush();

        /*
        List<byte> packetToSend = new List<byte>();
        packetToSend.Add(0); //opcode (opcode size is only 1 byte)
        groupAdd(ref packetToSend, BitConverter.GetBytes(0)); //x, y, z
        groupAdd(ref packetToSend, BitConverter.GetBytes(0));
        groupAdd(ref packetToSend, BitConverter.GetBytes(0));

        BinaryWriter writer = new BinaryWriter(ns);
        BinaryReader reader = new BinaryReader(ns);

        writer.Write(packetToSend.Count); //write length first
        writer.Write(packetToSend.ToArray()); //write actual packet that we sent its length of
        writer.Flush();
        */
        while (true)
        {
            if (!ns.DataAvailable)
                continue;

            int packetSize = reader.ReadInt32();
            byte[] packet = reader.ReadBytes(packetSize);

            //Debug.Log("PACKET SIZE: " + packetSize);
            if (packet[0] == 0) //opcode for getChunk
            {
                int chunkX = BitConverter.ToInt32(Slice(ref packet, 1, 5), 0);
                int chunkY = BitConverter.ToInt32(Slice(ref packet, 5, 9), 0);
                int chunkZ = BitConverter.ToInt32(Slice(ref packet, 9, 13), 0);

                //chunkY serves no use but maybe later it will..?
                Vector2 chunkPos = new Vector2(chunkX, chunkZ);

                //Debug.Log("Loading Chunk at " + new Vector3(chunkX, chunkY, chunkZ));

                byte[] uncompressedData = uncompressChunk(Slice(ref packet, 13, packet.Length));

                int blockNum = 0;
                byte[,,] chunkData = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                for (int x = 0; x < VoxelData.chunkWidth; x++)
                {
                    for (int y = 0; y < VoxelData.chunkHeight; y++)
                    {
                        for (int z = 0; z < VoxelData.chunkWidth; z++)
                        {
                            if (blockNum == uncompressedData.Length)
                                break;

                            chunkData[x, y, z] = uncompressedData[blockNum];
                            blockNum++;
                        }
                    }
                }

                watch.Stop();

                //Debug.Log("Uncompressing took "+watch.ElapsedMilliseconds+"ms");
                watch.Reset();

                //feels repetitve to store position in chunk object, if you know how to get it from the dictionary you already know its position..
                Chunk c = new Chunk(chunkPos, this);
                c.chunkData = chunkData;

                Task.Run(() => {
                    var threadWatch = new System.Diagnostics.Stopwatch();
                    threadWatch.Start();
                    c.buildMeshData();
                    threadWatch.Stop();
                    //Debug.Log("building verts and tris and uvs took "+threadWatch.ElapsedMilliseconds+"ms");

                    chunkDictionary.TryAdd(chunkPos, c);
                    chunksToBuild.Enqueue(chunkPos);
                });

            }
        }
    }

    private static int to1D(int x, int y, int z)
    {
        return (z * VoxelData.chunkWidth * VoxelData.chunkHeight) + (y * VoxelData.chunkWidth) + x;
    }

    //start: inclusive, end: exclusive
    T[] Slice<T>(ref T[] source, int start, int end)
    {
        if (end < 0)
            end = source.Length + end;

        int len = end - start;

        T[] res = new T[len];
        for (int i = 0; i < len; i++)
        {
            res[i] = source[i + start];
        }

        return res;
    }

    byte[] uncompressChunk(byte[] chunkData)
    {
        List<byte> uncompressedChunk = new List<byte>();

        for (int i = 0; i < chunkData.Length; i += 2)
        {
            for (int j = 0; j <= chunkData[i + 1]; j++)
                uncompressedChunk.Add(chunkData[i]);
        }

        return uncompressedChunk.ToArray();
    }

    void groupAdd<T>(ref List<T> list, T[] array)
    {
        foreach (T index in array)
        {
            list.Add(index);
        }
    }

}