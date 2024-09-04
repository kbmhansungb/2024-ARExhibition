using System;
using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WebCamSample
    : MonoBehaviour
{

    //// 웹캠 피드를 표시할 RawImage UI 요소 연결
    [SerializeField] private RawImage rawImage;

    private WebCamTexture webCamTexture;

    void Start()
    {
        // Check if the user has authorized the use of the webcam
        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            StartCamera();
        }
        else
        {
            StartCoroutine(RequestCameraAuthorization());
        }
    }

    private IEnumerator RequestCameraAuthorization()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            StartCamera();
        }
        else
        {
            Debug.LogWarning("Webcam authorization denied");
        }
    }

    private void StartCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            webCamTexture = new WebCamTexture(devices[0].name);
            rawImage.texture = webCamTexture;
            webCamTexture.Play();
        }
        else
        {
            Debug.LogWarning("No webcam found");
        }
    }

    void OnDestroy()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }
    }
}
