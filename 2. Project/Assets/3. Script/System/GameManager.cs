using ImageTracking.Model;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    [Header("GameManager")]
    [SerializeField] private ImageTracker imageTracker;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RawImage rawImage;

    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform trackingObject;

    private WebCamTexture webCamTexture;
    private Vector2 screenSize;

    public void Initialize()
    {
        imageTracker.Initialize();
    }

    public void Start()
    {
        StartCamera();
    }

    public void StartCamera()
    {
        StartCoroutine(StartCameraCoroutine());
    }

    private IEnumerator StartCameraCoroutine()
    {
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        webCamTexture = new WebCamTexture();
        rawImage.texture = webCamTexture;

        // 카메라 크기를 화면 크기에 맞게 조정합니다.
        screenSize = new Vector2(Screen.width/2, Screen.height/2);
        webCamTexture.requestedWidth = (int)screenSize.x;
        webCamTexture.requestedHeight = (int)screenSize.y;
        webCamTexture.Play();
    }

    public void Update()
    {
        if (webCamTexture == null || !webCamTexture.isPlaying)
        {
            return;
        }

        // 비디오 프레임을 받아옴
        Mat frame = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
        Utils.webCamTextureToMat(webCamTexture, frame);

        // 프레임 제공자로 부터 프레임을 받아서 이미지 매칭을 수행합니다.
        float focalLength = canvas.planeDistance / canvas.transform.localScale.x;
        var results = imageTracker.MatchFrame(frame, focalLength);

        var result = results[0];
        // 매칭 결과와, 포지션, 로테이션을 콘솔에 출력합니다.
        string resultString = $"MatchRatio: {result.MatchRatio}\n" +
                              $"Translation: {result.Translation}\n" +
                              $"EulerRotation: {result.EulerRotation}";
        Debug.Log(resultString);

        // 트래킹 오브젝트를 이동시킵니다.
        trackingObject.localPosition = result.Translation;
        trackingObject.localEulerAngles = result.EulerRotation;
    }
}
