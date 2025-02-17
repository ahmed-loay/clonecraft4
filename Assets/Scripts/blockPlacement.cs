using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class blockPlacement : MonoBehaviour
{
    public Chunk chunk;
    byte buildingBlock = (byte) blockType.BlockType.IronOre;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            buildingBlock = (byte)blockType.BlockType.Bricks;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            buildingBlock = (byte)blockType.BlockType.WoodenPlanks;
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            buildingBlock = (byte)blockType.BlockType.Stone;
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            buildingBlock = (byte)blockType.BlockType.Cobblestone;

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 blockPos = floorVec3(hit.point + hit.normal / 2);
                //Check if block pos equals player's head or leg position, if yes prevent building block
                if(floorVec3(transform.position) != blockPos && floorVec3(transform.position + Vector3.up) != blockPos)
                    chunk.setVoxel(blockPos, buildingBlock); //2 is grass
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 blockPos = floorVec3(hit.point - hit.normal / 2);
                chunk.setVoxel(blockPos, 0); //0 is reserved for air
            }
        }
    }

    Vector3 floorVec3(Vector3 vec)
    {
        return new Vector3(Mathf.Floor(vec.x), Mathf.Floor(vec.y), Mathf.Floor(vec.z));
    }
}
