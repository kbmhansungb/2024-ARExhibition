using UnityEngine;
using UnityEngine.UI;

public class WebGLCamSample : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;

    private void Update()
    {
        if (WebCamTexture.devices.Length == 0) return;

        var webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name);
        rawImage.texture = webCamTexture;
        webCamTexture.Play();
    }
}
