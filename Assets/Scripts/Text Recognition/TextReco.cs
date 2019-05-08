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
        Debug.Log("TR: Google Request submitted.");
        //byte[] bytes = File.ReadAllBytes(@"C:\Users\vrab\Desktop\AR for VIPs\Mesh Manipulation\Mesh-Manipulation\textImage.jpg");
        //Image img = Image.FromFile(@"C:\Users\vrab\Desktop\AR for VIPs\Mesh Manipulation\Mesh-Manipulation\textImage.jpg");
        // Convert to Bse64 String
        string base64Image = Convert.ToBase64String(image);
        Debug.Log("TR: base64 image sent.");

        DownloadHandler download = new DownloadHandlerBuffer();
        Debug.Log("TR: Download Handler set.");

        // Create JSON
        string json = "{\"requests\": [{\"image\": {\"content\": \"" + base64Image + "\"},\"features\": [{\"type\": \"TEXT_DETECTION\",\"maxResults\": 1}]}]}";
        byte[] content = Encoding.UTF8.GetBytes(json);
        Debug.Log("TR: JSON string created.");

        // Enter the url to your google vision api account here:
        string url = "https://vision.googleapis.com/v1/images:annotate?key=AIzaSyBTZXi8lZ9CAkgETGYBb3G9A7mzylWMmLQ";  //Manish's API
        //string url = "https://vision.googleapis.com/v1/images:annotate?key=ebd483e2c4d642c9cec634bc508fe095c557f289/"; //Rajan's API

        // Request to API
        var header = new Dictionary<string, string>() {
            { "Content-Type", "application/json" }
        };
       // Debug.Log("TR: Request to API (header) sent");

        var data = Encoding.UTF8.GetBytes(json);
        //Debug.Log("TR: data encoded.");

        WWW www = new WWW(url, data, header);
        //Debug.Log("TR: www created.");
        

        // Send API
        yield return www;
        Debug.Log("TR: www returned.");

        if (www.error == null)
        {
            string respJson = www.text;
            Debug.Log("TR response JSON received.");

            //Debug.Log(respJson);

            // Indicate when no text is detected
            if (respJson.Contains("textAnnotations") && !respJson.Contains("error"))
            {
                Debug.Log("TR: Text detected.");
                GetComponent<IconManager>().CreateIcons(respJson);
            }

            else if (!respJson.Contains("textAnnotations"))
            {
                {
                    Debug.Log("No text found");
                    this.transform.GetComponent<TextToSpeechGoogle>().playTextGoogle("No text found.");
                }
            }

            else Debug.Log("TR: Strange json response.");
        }

        else
        {
            Debug.Log("TR: www error found: " + www.error);
            if (www.text.Length <= 480)
            {
                Debug.Log("TR: www.text: " + www.text);
            }
            else
            {
                Debug.Log("TR: www.text length = " + www.text.Length);
            }
            
        }


    }
}
