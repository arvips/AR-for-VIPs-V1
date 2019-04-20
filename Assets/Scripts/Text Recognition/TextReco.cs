using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using System;
using System.IO;

public class TextReco : MonoBehaviour {

    public IEnumerator GoogleRequest(byte[] image)
    {
        Debug.Log("google request");
        //byte[] bytes = File.ReadAllBytes(@"C:\Users\vrab\Desktop\AR for VIPs\Mesh Manipulation\Mesh-Manipulation\textImage.jpg");
        //Image img = Image.FromFile(@"C:\Users\vrab\Desktop\AR for VIPs\Mesh Manipulation\Mesh-Manipulation\textImage.jpg");
        // Convert to Bse64 String
        string base64Image = Convert.ToBase64String(image);

        DownloadHandler download = new DownloadHandlerBuffer();

        // Create JSON
        string json = "{\"requests\": [{\"image\": {\"content\": \"" + base64Image + "\"},\"features\": [{\"type\": \"TEXT_DETECTION\",\"maxResults\": 1}]}]}";
        byte[] content = Encoding.UTF8.GetBytes(json);

        // Enter the url to your google vision api account here:
        string url = "https://vision.googleapis.com/v1/images:annotate?key=AIzaSyBTZXi8lZ9CAkgETGYBb3G9A7mzylWMmLQ";

        // Request to API
        var header = new Dictionary<string, string>() {
            { "Content-Type", "application/json" }
        };

        var data = Encoding.UTF8.GetBytes(json);
        WWW www = new WWW(url, data, header);

        // Send API
        yield return www;

        if (www.error == null)
        {
            string respJson = www.text;
            //Debug.Log(respJson);

            // Indicate when no text is detected
            if (respJson.Contains("textAnnotations") && !respJson.Contains("error"))
            {
                GetComponent<IconManager>().CreateIcons(respJson);
            }
        }


    }
}
