using ImageTracking.Model;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using System.Collections;
using System.Collections.Generic;
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

        WebCamDevice[] devices = WebCamTexture.devices;
        WebCamDevice selectDevice = devices[0];
        // 후면 카메라를 찾습니다.
        foreach (var device in devices)
        {
            if (!device.isFrontFacing)
            {
                selectDevice = device;
                break;
            }
        }

        webCamTexture = new WebCamTexture(selectDevice.name);
        rawImage.texture = webCamTexture;

        // 카메라 크기를 화면 크기에 맞게 조정합니다.
        //screenSize = new Vector2(Screen.width, Screen.height);
        screenSize = new Vector2(768, 1024);
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
        //// 매칭 결과와, 포지션, 로테이션을 콘솔에 출력합니다.
        //string resultString = $"MatchRatio: {result.MatchRatio}\n" +
        //                      $"Translation: {result.Translation}\n" +
        //                      $"EulerRotation: {result.EulerRotation}";
        //Debug.Log(resultString);

        if (result.IsTracking)
        {
            isTracking = true;
            failCount = 0;

            translations.Add(result.Translation);
            //eulerRotations.Add(result.EulerRotation);
            forwards.Add(result.Foward);
            ups.Add(result.Up);

            if (translations.Count > 3)
            {
                translations.RemoveAt(0);
                //eulerRotations.RemoveAt(0);
                forwards.RemoveAt(0);
                ups.RemoveAt(0);
            }
        }
        else
        {
            failCount++;
            if (failCount > 3)
            {
                isTracking = false;
                failCount = 0;

                translations.Clear();
                //eulerRotations.Clear();
                forwards.Clear();
                ups.Clear();
            }
        }

        //// 트래킹 오브젝트를 이동시킵니다.
        //trackingObject.localPosition = result.Translation;
        //trackingObject.localEulerAngles = result.EulerRotation;

        Vector3 averageTranslation = Vector3.zero;
        //Vector3 averageEulerRotation = Vector3.zero;
        Vector3 averageForward = Vector3.zero;
        Vector3 averageUp = Vector3.zero;

        if (translations.Count > 0)
        {
            foreach (var translation in translations)
            {
                averageTranslation += translation;
            }
            averageTranslation /= translations.Count;

            //foreach (var eulerRotation in eulerRotations)
            //{
            //    averageEulerRotation += eulerRotation;
            //}
            //averageEulerRotation /= translations.Count;

            foreach (var forward in forwards)
            {
                averageForward += forward;
            }
            averageForward /= translations.Count;

            foreach (var up in ups)
            {
                averageUp += up;
            }
            averageUp /= translations.Count;
        }

        trackingObject.localPosition = averageTranslation;
        //trackingObject.localEulerAngles = averageEulerRotation;
        Vector3 eulerRotation = Quaternion.LookRotation(averageForward, averageUp).eulerAngles;
        trackingObject.localEulerAngles = eulerRotation;
    }

    bool isTracking = false;
    List<Vector3> translations = new List<Vector3>();
    //List<Vector3> eulerRotations = new List<Vector3>();
    List<Vector3> forwards = new List<Vector3>();
    List<Vector3> ups = new List<Vector3>();

    int failCount = 0;
}
