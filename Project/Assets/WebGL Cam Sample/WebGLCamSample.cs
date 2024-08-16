using System;
using UnityEngine;
using UnityEngine.UI;

public class WebGLCamSample : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;

    // JavaScript에서 호출되는 메서드
    public void OnCameraImageCaptured(string base64Image)
    {
        // Base64 문자열에서 이미지 데이터를 디코딩
        byte[] imageBytes = Convert.FromBase64String(base64Image.Replace("data:image/png;base64,", ""));

        if (rawImage.texture != null)
        {
            Destroy(rawImage.texture);
        }

        // Texture2D 생성
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageBytes);

        // 원하는 곳에 텍스처 적용
        rawImage.texture = texture;
    }
}
