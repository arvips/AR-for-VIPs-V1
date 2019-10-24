using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
//using System.Net.HTTP;

public class TextReco : MonoBehaviour
{

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
        string url = "https://vision.googleapis.com/v1/images:annotate?key=AIzaSyD_RSXp5Mugky12sYxXK6z1D2xCedKSCCc";  //Manish's API
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

    public async IEnumerator AzureRequest(byte[] image)
    {
        string subscriptionKey = "bc12a427cee04549a3014196d8e12078";
        string endpoint = "https://arvips.cognitiveservices.azure.com/";
        string uriBase = endpoint + "vision/v2.1/ocr";

        try
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add(
                "Ocp-Apim-Subscription-Key", subscriptionKey);

            // Request parameters. 
            // The language parameter doesn't specify a language, so the 
            // method detects it automatically.
            // The detectOrientation parameter is set to true, so the method detects and
            // and corrects text orientation before detecting text.
            string requestParameters = "language=unk&detectOrientation=true";

            // Assemble the URI for the REST API method.
            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;

            byte[] byteData = image;


            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses the "application/octet-stream" content type.
                // The other content types you can use are "application/json"
                // and "multipart/form-data".
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");

                // Asynchronously call the REST API method.
                response = await client.PostAsync(uri, content);
            }

            // Asynchronously get the JSON response.
            string contentString = await response.Content.ReadAsStringAsync();

            // Indicate when no text is detected
            if (respJson.Contains("textAnnotations") && !respJson.Contains("error"))
            {
                Debug.Log("TR: Text detected.");
                GetComponent<IconManager>().CreateIcons(respJson);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("\n" + e.Message);
        }
    }
}
