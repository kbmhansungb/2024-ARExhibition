using System;
using UnityEngine;
using UnityEngine.UI;

public class WebGLCamSample : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;

    // JavaScript���� ȣ��Ǵ� �޼���
    public void OnCameraImageCaptured(string base64Image)
    {
        // Base64 ���ڿ����� �̹��� �����͸� ���ڵ�
        byte[] imageBytes = Convert.FromBase64String(base64Image.Replace("data:image/png;base64,", ""));

        if (rawImage.texture != null)
        {
            Destroy(rawImage.texture);
        }

        // Texture2D ����
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageBytes);

        // ���ϴ� ���� �ؽ�ó ����
        rawImage.texture = texture;
    }
}
