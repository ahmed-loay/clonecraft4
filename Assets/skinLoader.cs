using UnityEngine;
using UnityEngine.Networking;

using System.Dynamic;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

public class skinLoader : MonoBehaviour
{
    public GameObject stevePrefab;
    public GameObject alexPrefab;

    private GameObject currObject;

    public string username;

    readonly string UUIDUrl = "https://api.mojang.com/users/profiles/minecraft/"; //add username at the end
    readonly string profileUrl = "https://sessionserver.mojang.com/session/minecraft/profile/"; //add uuid

    void Start()
    {
        //get UUID of username
        string uuidRes = fetch(UUIDUrl + username);
        string uuid = JsonConvert.DeserializeObject<dynamic>(uuidRes)["id"];

        //get profile based on that UUID
        string profileRes = fetch(profileUrl + uuid);

        //get base64-encoded texture json
        string base64TextureJson = JsonConvert.DeserializeObject<dynamic>(profileRes)["properties"][0]["value"];

        //decode json
        byte[] data = System.Convert.FromBase64String(base64TextureJson);
        string stringTextureJson = Encoding.UTF8.GetString(data);

        //get texture url from decoded base-64
        string textureUrl = JsonConvert.DeserializeObject<dynamic>(stringTextureJson)["textures"]["SKIN"]["url"];

        //check whether if the texture is steve, or alex
        //we check by seeing if metadata exists, if it does its a steve model, otherwise its alex
        if(JsonConvert.DeserializeObject<dynamic>(stringTextureJson)["textures"]["SKIN"]["metadata"] != null)
            Debug.Log("ITS AN ALEX MODEL");

        //Debug.Log(textureUrl);

        StartCoroutine(applyTexture(textureUrl));

    }

    IEnumerator applyTexture(string textureUrl)
    {
        UnityWebRequest req = UnityWebRequestTexture.GetTexture(textureUrl);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(req.error);
        }
        else
        {
            Texture downloadedTexture = ((DownloadHandlerTexture)req.downloadHandler).texture;
            downloadedTexture.filterMode = FilterMode.Point;
            downloadedTexture.wrapMode = TextureWrapMode.Clamp;

            GetComponent<MeshRenderer>().material.mainTexture = downloadedTexture;
        }
    }

    string fetch(string url)
    {
        WebRequest request = WebRequest.Create(url);
        HttpWebResponse res = (HttpWebResponse) request.GetResponse();
        StreamReader reader = new StreamReader(res.GetResponseStream());

        string resString = reader.ReadToEnd();

        reader.Close();
        res.Close();

        return resString;
    }
}
