[System.Serializable]
public class blockType
{
    public string name;
    public bool isSolid;

    //index 0 = x, index 1 = y
    //instead of vectors to save myself from headaches
    public byte[] topFace;
    public byte[] bottomFace;
    public byte[] rightFace;
    public byte[] leftFace;
    public byte[] backFace;
    public byte[] frontFace;

    public byte[] getTexture(byte faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return topFace;
            case 1:
                return bottomFace;
            case 2:
                return rightFace;
            case 3:
                return leftFace;
            case 4:
                return backFace;
            case 5:
                return frontFace;
            default:
                {
                    UnityEngine.Debug.Log("Error in getTexture Function");
                    return bottomFace;
                }
        }
    }

    public enum BlockType : byte {
        Air = 0,
        Stone = 1,
        Grass = 2,
        Dirt = 3,
        WoodenPlanks = 4,
        Cobblestone = 5,
        Bricks = 6,
        IronOre = 7
    }

}
