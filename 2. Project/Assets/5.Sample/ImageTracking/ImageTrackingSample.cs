using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ImageTracking.Sample
{
    /// <summary>
    /// <para>트래킹 대상 데이터 입니다.</para>
    /// <para>* <see cref="TrackingTargetData"/>참고</para>
    /// </summary>
    [Serializable]
    public class TrackingTargetData
    {
        public Texture2D Texture;
        public Size RealImageSize;

        [NonSerialized] public Mat ImageMat;
        [NonSerialized] public MatOfKeyPoint KeyPoints;
        [NonSerialized] public Mat Descriptors;

        public List<Vector3> localPositions = new List<Vector3>();
        public List<Vector3> eulerRotations = new List<Vector3>();

        public void Initialize()
        {
            KeyPoints = new MatOfKeyPoint();
            Descriptors = new Mat();
            ImageMat = new Mat(Texture.height, Texture.width, CvType.CV_8UC4);
            Utils.texture2DToMat(Texture, ImageMat);

            // RGB -> GRAY로 변환
            Imgproc.cvtColor(ImageMat, ImageMat, Imgproc.COLOR_RGBA2GRAY);

            // 특징점 검출하고 계산
            ORB orb = ORB.create();
            orb.detectAndCompute(ImageMat, new Mat(), KeyPoints, Descriptors);
        }
    }

    public class ImageTrackingSample : MonoBehaviour
    {
        [Header("ImageTrackingSample/Tracking targets")]
        [SerializeField] private TrackingTargetData trackingTarget;

        List<Vector3> points;

        [Header("ImageTrackingSample/Reference")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private RawImage rawImage;
        [SerializeField] private Transform targetTransform;
        
        public float focalLength = 100;

        [Header("ImageTrackingSample/WebCamTexture")]
        [SerializeField] private Size FrameSize = new Size(1280, 720);
        private WebCamTexture webCamTexture; // VideoCapture의 경우 Web에서 카메라 접속이 안되어 WebCamTexture로 대체
        private ORB orb;
        private DescriptorMatcher matcher;

        [Header("ImageTrackingSample/Debug")]
        [SerializeField] private TextMeshProUGUI debugText;


        private void Start()
        {
            // ORB 검출기와 Matcher를 초기화합니다.
            {
                orb = ORB.create();
                matcher = DescriptorMatcher.create(DescriptorMatcher.BRUTEFORCE_HAMMINGLUT);
            }

            // 트래킹 대상 이미지를 초기화합니다.
            trackingTarget.Initialize();

            Utils.setDebugMode(true);

            // 사용자가 웹캠 사용을 허가했는지 확인
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
                //// 500x500으로 크기 조정 // 동작 안함
                webCamTexture.requestedWidth = (int)FrameSize.width;
                webCamTexture.requestedHeight = (int)FrameSize.height;

                webCamTexture.Play();
            }
            else
            {
                Debug.LogWarning("No webcam found");
            }
        }

        private void Update()
        {
            // 웹캠이 정지되어 있으면 리턴
            if (webCamTexture == null || !webCamTexture.isPlaying)
            {
                return;
            }

            // 프레임이 업데이트 되지 않았으면 리턴
            if (webCamTexture.didUpdateThisFrame)
            {
                return;
            }

            debugText.text = "";

            // 비디오 프레임을 받아옴
            Mat frame = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
            Utils.webCamTextureToMat(webCamTexture, frame);

            // grayscale 이미지 생성
            Mat grayFrame = new Mat();
            Imgproc.cvtColor(frame, grayFrame, Imgproc.COLOR_RGB2GRAY);

            // 특징점 검출하고 계산
            MatOfKeyPoint keyPoints = new MatOfKeyPoint();
            Mat descriptors = new Mat();
            orb.detectAndCompute(grayFrame, new Mat(), keyPoints, descriptors);

            // 매칭
            MatOfDMatch matches = new MatOfDMatch();
            matcher.match(trackingTarget.Descriptors, descriptors, matches);

            // 매칭 결과를 거리로 정렬
            List<DMatch> matchesList = matches.toList();
            List<DMatch> goodMatches = new List<DMatch>();
            for (int j = 0; j < matchesList.Count; j++)
            {
                var match = matchesList[j];
                if (match.distance < 50)
                {
                    goodMatches.Add(match);
                }
            }

            if (goodMatches.Count < 4)
            {
                UpdateITexture(frame);
                return;
            }

            // homography 계산
            List<Point> srcPoints = new List<Point>();
            List<Point> dstPoints = new List<Point>();
            foreach (var match in goodMatches)
            {
                srcPoints.Add(trackingTarget.KeyPoints.toArray()[match.queryIdx].pt);
                dstPoints.Add(keyPoints.toArray()[match.trainIdx].pt);
            }

            MatOfPoint2f srcPointsMat = new MatOfPoint2f();
            srcPointsMat.fromList(srcPoints);
            MatOfPoint2f dstPointsMat = new MatOfPoint2f();
            dstPointsMat.fromList(dstPoints);

            Mat homography = Calib3d.findHomography(srcPointsMat, dstPointsMat, Calib3d.RANSAC, 5);

            if (!homography.empty())
            {
                // 트래킹 대상의 위치와 회전을 계산
                MatOfPoint2f trackingTargetCorners
                    = new MatOfPoint2f(
                        new Point(0, 0),
                        new Point(trackingTarget.Texture.width, 0),
                        new Point(trackingTarget.Texture.width, trackingTarget.Texture.height),
                        new Point(0, trackingTarget.Texture.height),


                        new Point(trackingTarget.Texture.width / 2, trackingTarget.Texture.height / 2), // center
                        new Point(trackingTarget.Texture.width / 2 + 100, trackingTarget.Texture.height / 2), 
                        new Point(trackingTarget.Texture.width / 2, trackingTarget.Texture.height / 2 - 100)
                        );

                debugText.text = $"Homography: {homography.dump()}";
                debugText.text += $"\nTrackingTargetCorners: {trackingTargetCorners.dump()}";

                MatOfPoint2f trackingTargetCornersTransformed = new MatOfPoint2f();
                Core.perspectiveTransform(trackingTargetCorners, trackingTargetCornersTransformed, homography);

                var results = trackingTargetCornersTransformed.toList();

                // 이미지 경계선을 그림
                Imgproc.line(frame, results[0], results[1], new Scalar(255, 0, 0), 2);
                Imgproc.line(frame, results[1], results[2], new Scalar(255, 0, 0), 2);
                Imgproc.line(frame, results[2], results[3], new Scalar(255, 0, 0), 2);
                Imgproc.line(frame, results[3], results[0], new Scalar(255, 0, 0), 2);

                // 대각을 그림
                Imgproc.line(frame, results[0], results[2], new Scalar(255, 0, 0), 1);
                Imgproc.line(frame, results[1], results[3], new Scalar(255, 0, 0), 1);


                // 중심점을 그림
                Imgproc.circle(frame, results[4], 5, new Scalar(0, 255, 0), 2);

                // draw vector
                Imgproc.line(frame, results[4], results[5], new Scalar(0, 255, 0), 2);
                Imgproc.line(frame, results[4], results[6], new Scalar(0, 255, 0), 2);

                debugText.text += $"\nTrackingTargetCornersTransformed: {trackingTargetCornersTransformed.dump()}";

                // 기본 카메라 메트릭스 정의
                //float focalLength = 100;
                Mat cameraMatrix = new Mat(3, 3, CvType.CV_64FC1);
                cameraMatrix.put(0, 0, focalLength);
                cameraMatrix.put(1, 1, focalLength);
                cameraMatrix.put(0, 2, frame.width() / 2);
                cameraMatrix.put(1, 2, frame.height() / 2);
                cameraMatrix.put(2, 2, 1);

                // 카메라 메트릭스를 이용하여 homography를 변환
                Mat H1 = cameraMatrix.inv() * homography;

                debugText.text += $"\nHomography: {H1.dump()}";

                // 코너 포인트를 정의하고 H1을 이용하여 변환
                this.points = new List<Vector3>();
                foreach (var point in trackingTargetCorners.toList())
                {
                    Mat resultPoints = H1 * new MatOfDouble(point.x, point.y, 1);
                    Vector3 localPosition = new Vector3((float)resultPoints.get(0, 0)[0], (float)resultPoints.get(1, 0)[0], (float)resultPoints.get(2, 0)[0]);

                    // unity 좌표계로 변환
                    localPosition = new Vector3(localPosition.x, -localPosition.y, localPosition.z);
                    points.Add(localPosition);
                }

                // 각 변의 길이를 구함
                float width1 = Vector3.Distance(points[0], points[1]);  
                float width2 = Vector3.Distance(points[2], points[3]);      
                float height1 = Vector3.Distance(points[0], points[3]); 
                float height2 = Vector3.Distance(points[1], points[2]);

                // 이미지의 실제 크기와 비율의 평균을 구함
                  float toRealRatio = (((float)trackingTarget.RealImageSize.width / width1) + ((float)trackingTarget.RealImageSize.width / width2) + ((float)trackingTarget.RealImageSize.height / height1) + ((float)trackingTarget.RealImageSize.height / height2)) / 4;
                // 실제 크기를 가지도록 스케일을 조정  
                for (int i = 0; i < points.Count; i++)
                {
                    points[i] = points[i] * toRealRatio;
                }

                // 카메라를 기준으로 world 좌표로 변환
                for (int i = 0; i < points.Count; i++)
                {
                    Vector3 worldPOsition = mainCamera.transform.TransformPoint(points[i]);
                    points[i] = worldPOsition;  
                }

                debugText.text += $"\nPoints: {string.Join(", ", points)}";
            }

            UpdateITexture(frame);
        }

        private void OnDrawGizmos()
        {
            if (points == null)
            {
                return;
            }

            //Gizmos.color = Color.red;
            //for (int i = 0; i < points.Count; i++)
            //{
            //    Gizmos.DrawSphere(points[i], 0.1f);
            //}

            // 사각형 그리기
            Gizmos.color = Color.green;
            Gizmos.DrawLine(points[0], points[1]);
            Gizmos.DrawLine(points[1], points[2]);
            Gizmos.DrawLine(points[2], points[3]);
            Gizmos.DrawLine(points[3], points[0]);

            // 대각선 그리기
            Gizmos.DrawLine(points[0], points[2]);
            Gizmos.DrawLine(points[1], points[3]);

            // 중심점 그리기
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(points[4], 0.1f);

            // 벡터 그리기
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(points[4], points[5]);
            Gizmos.DrawLine(points[4], points[6]);

            // 카메라로부터 사각형의 경계선을 지나는 선 그리기
            Vector3 cameraPosition = mainCamera.transform.position;

            Vector3 p0Vec = (points[0] - cameraPosition).normalized;
            Vector3 p1Vec = (points[1] - cameraPosition).normalized;
            Vector3 p2Vec = (points[2] - cameraPosition).normalized;
            Vector3 p3Vec = (points[3] - cameraPosition).normalized;

            float length = 1000;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(cameraPosition, cameraPosition + p0Vec * length);
            Gizmos.DrawLine(cameraPosition, cameraPosition + p1Vec * length);
            Gizmos.DrawLine(cameraPosition, cameraPosition + p2Vec * length);
            Gizmos.DrawLine(cameraPosition, cameraPosition + p3Vec * length);
        }

        private void OnDestroy()
        {
            if (rawImage.texture != null)
            {
                Destroy(rawImage.texture);
            }

            if (webCamTexture != null)
            {
                webCamTexture.Stop();
            }
        }

        /// <summary>
        /// 텍스쳐를 받아서 RawImage에 적용
        /// </summary>
        /// <param name="newTexture"></param>
        private void UpdateITexture(Mat frame)
        {
            if (rawImage.texture != null)
            {
                Destroy(rawImage.texture);
                rawImage.texture = null;
            }

            // 프레임을 텍스쳐로 변환
            Texture2D texture = new Texture2D(frame.cols(), frame.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(frame, texture);

            rawImage.texture = texture;
        }
    }

}
