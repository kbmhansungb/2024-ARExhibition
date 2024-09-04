using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.VideoioModule;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ImageTracking.Sample
{
    [Serializable]
    public class TrackingTarget
    {
        public Texture2D texture;
        public Mat imageMat;
        public MatOfKeyPoint keyPoints;
        public Mat descriptors;
    }

    public class ImageTrackingSample : MonoBehaviour
    {
        [Header("ImageTrackingSample/WebCamTexture")]
        private WebCamTexture webCamTexture;

        [Header("ImageTrackingSample/Reference")]
        [SerializeField] private RawImage rawImage;

        [Header("ImageTrackingSample/Tracking targets")]
        [SerializeField] private List<Texture2D> imageReferences;
        private List<TrackingTarget> trackingTargets = new List<TrackingTarget>();

        //[SerializeField] private Size reducedSize = new Size(320, 240);
        private Size sourceSize;
        private Size resizeScale;

        //private VideoCapture videoCapture;
        private ORB orb;
        private DescriptorMatcher matcher;

        private void Start()
        {
            //// 비디오 캡쳐 객체 생성
            //videoCapture = new VideoCapture(0);

            // ORB 검출기를 초기화합니다.
            orb = ORB.create();

            // FLANN 기반 매처를 초기화합니다.
            matcher = DescriptorMatcher.create(DescriptorMatcher.BRUTEFORCE_HAMMINGLUT);

            // 트래킹 대상 이미지를 초기화합니다.
            foreach (var texture in imageReferences)
            {
                MatOfKeyPoint keyPoints = new MatOfKeyPoint();
                Mat descriptors = new Mat();
                Mat imageMat = new Mat(texture.height, texture.width, CvType.CV_8UC4);
                Utils.texture2DToMat(texture, imageMat);

                // RGB -> GRAY로 변환
                Imgproc.cvtColor(imageMat, imageMat, Imgproc.COLOR_RGBA2GRAY);

                // 최적화를 위해 이미지 크기를 줄임
                Size imageSize = new Size(imageMat.cols(), imageMat.rows());
                //double matchScale = Math.Min(reducedSize.width / imageSize.width, reducedSize.height / imageSize.height);
                //Imgproc.resize(imageMat, imageMat, reducedSize * matchScale);

                orb.detectAndCompute(imageMat, new Mat(), keyPoints, descriptors);

                trackingTargets.Add(new TrackingTarget
                {
                    texture = texture,
                    imageMat = imageMat,
                    keyPoints = keyPoints,
                    descriptors = descriptors
                });
            }

            Utils.setDebugMode(true);


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
                //rawImage.texture = webCamTexture;
                webCamTexture.Play();
            }
            else
            {
                Debug.LogWarning("No webcam found");
            }
        }

        private void Update()
        {
            //// 이미지 특성 추출 테스트
            //{
            //    var trackingTarget = trackingTargets[0];
            //
            //    // 특징점 그림
            //    Mat resultMat = trackingTarget.imageMat.clone();
            //    Features2d.drawKeypoints(trackingTarget.imageMat, trackingTarget.keyPoints, resultMat);
            //    UpdateITexture(resultMat);
            //
            //    return;
            //}

            // 비디오 프레임을 받아옴
            Mat frame = null;
            if (webCamTexture.didUpdateThisFrame)
            {
                frame = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webCamTexture, frame);
            }
            else
            {
                return;
            }
            sourceSize = new Size(frame.cols(), frame.rows());

            ////
            // 특징점 검출

            //// BRG -> RGB로 변환
            //Imgproc.cvtColor(frame, frame, Imgproc.COLOR_BGR2RGB);

            // grayscale 이미지 생성
            Mat grayFrame = new Mat();
            Imgproc.cvtColor(frame, grayFrame, Imgproc.COLOR_RGB2GRAY);

            // 최적화를 위해 이미지 크기를 줄임
            //Imgproc.resize(grayFrame, grayFrame, reducedSize);
            //resizeScale = new Size(sourceSize.width / reducedSize.width, sourceSize.height / reducedSize.height);

            // 특징점 검출하고 계산
            MatOfKeyPoint keyPoints = new MatOfKeyPoint();
            Mat descriptors = new Mat();
            orb.detectAndCompute(grayFrame, new Mat(), keyPoints, descriptors);

            //// 
            // 가장 잘 매칭되는 이미지 찾습니다.
            int bestMatchIndex = -1;
            int maxGoodMatchCount = 0;
            Mat bestHomography = null;
            for (int i = 0; i < trackingTargets.Count; i++)
            {
                var trackingTarget = trackingTargets[i];

                MatOfDMatch matches = new MatOfDMatch();
                matcher.match(trackingTarget.descriptors, descriptors, matches);

                // 좋은 매칭점 찾기
                List<DMatch> matchesList = matches.toList();
                List<DMatch> goodMatches = new List<DMatch>();
                for (int j = 0; j < matchesList.Count; j++)
                {
                    if (matchesList[j].distance < 50)
                    {
                        goodMatches.Add(matchesList[j]);
                    }
                }

                if (maxGoodMatchCount < goodMatches.Count)
                {
                    maxGoodMatchCount = goodMatches.Count;
                    bestMatchIndex = i;

                    // 호모그래피를 찾기 위해서는 최소 4개의 매칭점이 필요합니다.
                    if (goodMatches.Count > 4)
                    {
                        // 매칭점을 이용하여 호모그래피를 찾습니다.
                        List<Point> srcPoints = new List<Point>();
                        List<Point> dstPoints = new List<Point>();
                        foreach (var match in goodMatches)
                        {
                            srcPoints.Add(trackingTarget.keyPoints.toArray()[match.queryIdx].pt);
                            dstPoints.Add(keyPoints.toArray()[match.trainIdx].pt);
                        }

                        MatOfPoint2f srcPointsMat = new MatOfPoint2f();
                        srcPointsMat.fromList(srcPoints);
                        MatOfPoint2f dstPointsMat = new MatOfPoint2f();
                        dstPointsMat.fromList(dstPoints);

                        bestHomography = Calib3d.findHomography(srcPointsMat, dstPointsMat, Calib3d.RANSAC, 5);
                    }
                }
            }

            Vector3 eulerRotation = new Vector3(0, 0, 0);
            Vector3 position = new Vector3(0, 0, 0);
            if (bestMatchIndex != -1 && bestHomography != null)
            {
                // 가장 유사한 이미지의 코너를 현재 프레임에 투영하여 그립니다
                var trackingTarget = trackingTargets[bestMatchIndex];
                int h = trackingTarget.imageMat.rows();
                int w = trackingTarget.imageMat.cols();
                List<Point> pts = new List<Point>
            {
                new Point(0, 0),
                new Point(0, h - 1),
                new Point(w - 1, h - 1),
                new Point(w - 1, 0)
            };
                MatOfPoint2f ptsMat = new MatOfPoint2f();
                ptsMat.fromList(pts);
                MatOfPoint2f dst = new MatOfPoint2f();
                Core.perspectiveTransform(ptsMat, dst, bestHomography);

                // 현재 프레임에 사각형을 그립니다
                Imgproc.polylines(frame, new List<MatOfPoint> { new MatOfPoint(dst.toArray()) }, true, new Scalar(0, 255, 0), 3, Imgproc.LINE_AA);
                Debug.Log("bestMatchIndex: " + bestMatchIndex);

                // 카메라 행렬
                float focalLength = 1.0f;
                Vector2 center = new Vector2(frame.cols() / 2, frame.rows() / 2);

                var cameraMatrix = new Mat(3, 3, CvType.CV_64FC1);
                cameraMatrix.put(0, 0, focalLength);
                cameraMatrix.put(0, 2, center.x);
                cameraMatrix.put(1, 1, focalLength);
                cameraMatrix.put(1, 2, center.y);
                cameraMatrix.put(2, 2, 1.0);

                // 객체의 3D 자표 정의
                MatOfPoint3f objPoints = new MatOfPoint3f();
                objPoints.fromArray(new Point3(0, 0, 0), new Point3(0, h - 1, 0), new Point3(w - 1, h - 1, 0), new Point3(w - 1, 0, 0));

                // 카메라 행렬을 이용하여 카메라 위치와 회전을 찾습니다.
                Mat rvec = new Mat();
                Mat tvec = new Mat();
                Calib3d.solvePnP(objPoints, dst, cameraMatrix, new MatOfDouble(), rvec, tvec);

                position = new Vector3((float)tvec.get(0, 0)[0], (float)tvec.get(1, 0)[0], (float)tvec.get(2, 0)[0]);
                eulerRotation = new Vector3((float)rvec.get(0, 0)[0], (float)rvec.get(1, 0)[0], (float)rvec.get(2, 0)[0]);

                Debug.Log("position: " + position + " eulerRotation: " + eulerRotation);
            }


            //////
            //// 특징점 그림
            //{
            //    // 특징점을 원래 이미지에 그리기 위해 크기를 원래 크기로 복원
            //    {
            //        var pointArray = keyPoints.toArray();
            //        for (int i = 0; i < pointArray.Length; i++)
            //        {
            //            pointArray[i].pt = new Point(pointArray[i].pt.x * resizeScale.width, pointArray[i].pt.y * resizeScale.height);
            //        }
            //        keyPoints.fromArray(pointArray);
            //    }

            //    // 특징점을 그림
            //    Features2d.drawKeypoints(frame, keyPoints, frame);
            //}

            // RawImage에 텍스쳐 적용
            UpdateITexture(frame);
        }

        private void OnDestroy()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
            }

            if (rawImage.texture != null)
            {
                Destroy(rawImage.texture);
                rawImage.texture = null;
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