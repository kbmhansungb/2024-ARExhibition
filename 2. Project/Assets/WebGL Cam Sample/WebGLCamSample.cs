using System;
using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WebGLCamSample : MonoBehaviour
{
    public void Update()
    {
        //byte[] buffer = TestLib.Class1.Test();

        //// buffer를 텍스쳐로 변환
        //Texture2D tex = new Texture2D(480, 320, TextureFormat.ETC2_RGBA8, false);
        //tex.LoadRawTextureData(buffer);
        //tex.Apply();

        //urlText.text = buffer.Length.ToString();
        //if (rawImage.texture != null)
        //{
        //    Destroy(rawImage.texture);
        //}
        //rawImage.texture = tex;
    }

    //// 웹캠 피드를 표시할 RawImage UI 요소 연결
    [SerializeField] private RawImage rawImage;
    [SerializeField] private TextMeshProUGUI urlText;

    //private WebCamTexture webCamTexture;

    //void Start()
    //{
    //    // Check if the user has authorized the use of the webcam
    //    if (Application.HasUserAuthorization(UserAuthorization.WebCam))
    //    {
    //        StartCamera();
    //    }
    //    else
    //    {
    //        StartCoroutine(RequestCameraAuthorization());
    //    }
    //}

    //private IEnumerator RequestCameraAuthorization()
    //{
    //    yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
    //    if (Application.HasUserAuthorization(UserAuthorization.WebCam))
    //    {
    //        StartCamera();
    //    }
    //    else
    //    {
    //        Debug.LogError("Webcam authorization denied");
    //    }
    //}

    //private void StartCamera()
    //{
    //    WebCamDevice[] devices = WebCamTexture.devices;
    //    if (devices.Length > 0)
    //    {
    //        webCamTexture = new WebCamTexture(devices[0].name);
    //        rawImage.texture = webCamTexture;
    //        webCamTexture.Play();
    //    }
    //    else
    //    {
    //        Debug.LogError("No webcam found");
    //    }
    //}

    //private void FixedUpdate()
    //{
    //    urlText.text = WebCamLib.GetName();
    //    Debug.Log(urlText.text);
    //}

    //void OnDestroy()
    //{
    //    if (webCamTexture != null)
    //    {
    //        webCamTexture.Stop();
    //    }
    //}
}
