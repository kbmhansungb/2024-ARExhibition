using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WebGLCamSample : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;

    private WebCamDevice[] devices;

    // Use this for initialization
    IEnumerator Start()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.Log("webcam found");
            devices = WebCamTexture.devices;
            for (int cameraIndex = 0; cameraIndex < devices.Length; ++cameraIndex)
            {
                Debug.Log("devices[cameraIndex].name:");
                Debug.Log(devices[cameraIndex].name);
                Debug.Log("devices[cameraIndex].isFrontFacing");
                Debug.Log(devices[cameraIndex].isFrontFacing);
            }
        }
        else
        {
            Debug.Log("no webcams found");
        }

        WebCamTexture webcamTexture = new WebCamTexture(devices[0].name);
        rawImage.texture = webcamTexture;
        rawImage.material.mainTexture = webcamTexture;
        webcamTexture.Play();
    }
}
