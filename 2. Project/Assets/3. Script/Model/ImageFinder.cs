using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImageTracking.Model
{
    /// <summary>
    /// <para>트래킹할 이미지 데이터 입니다.</para>
    /// </summary>
    [Serializable]
    public class TrackingTarget: IDisposable
    {
        public Texture2D ReferenceTexture;
        public Mat ImageMat;
        public MatOfKeyPoint KeyPoints;
        public Mat Descriptors;

        public void Dispose()
        {
            if (ReferenceTexture != null)
            {
                //UnityEngine.Object.Destroy(texture); // 래퍼런스 타입의 경우 Destroy를 사용하지 않습니다.
                ReferenceTexture = null;
            }

            if (ImageMat != null)
            {
                ImageMat.Dispose();
                ImageMat = null;
            }

            if (KeyPoints != null)
            {
                KeyPoints.Dispose();
                KeyPoints = null;
            }

            if (Descriptors != null)
            {
                Descriptors.Dispose();
                Descriptors = null;
            }
        }
    }

    /// <summary>
    /// <para>트래킹 결과 데이터 입니다.</para>
    /// </summary>
    [Serializable]
    public class TrackingResult
    {
        public List<DMatch> MatchList;
        public List<DMatch> GoodMatchList;

        public TrackingTarget TrackingTarget;

        public Vector3 EulerRotation;
        public Vector3 Translation;

        public int TotalMatches => MatchList.Count;
        public int GoodMatches => GoodMatchList.Count;
        public float MatchRatio => (float)GoodMatches / TotalMatches;
    }

    /// <summary>
    /// <para>프레임에 찾을 이미지들을 매칭하는 역할을 합니다.</para>
    /// </summary>
    public class ImageFinder : MonoBehaviour
    {
        [Header("ImageFinder")]
        [SerializeField] private List<Texture2D> imageReferences;
        private List<TrackingTarget> trackingTargets = new List<TrackingTarget>();

        ORB orb = ORB.create();
        DescriptorMatcher matcher = DescriptorMatcher.create(DescriptorMatcher.BRUTEFORCE_HAMMING);

        private List<TrackingResult> trackingResults = new List<TrackingResult>();

        public List<TrackingTarget> TrackingTargets { get => trackingTargets; }
        public List<TrackingResult> TrackingResults { get => trackingResults; }

        public void Initialize()
        {
            UpdateTrackingTarget();
        }

        /// <summary>
        /// <para>트래킹할 이미지 타겟을 업데이트 합니다.</para>
        /// </summary>
        public void UpdateTrackingTarget()
        {
            foreach (var imageReference in imageReferences)
            {
                // 이미지를 Mat으로 변환합니다.
                Mat imageMat = new Mat(imageReference.height, imageReference.width, CvType.CV_8UC4);
                Utils.texture2DToMat(imageReference, imageMat);

                // ORB로 특징점을 추출합니다.
                MatOfKeyPoint keyPoints = new MatOfKeyPoint();
                Mat descriptors = new Mat();

                orb.detectAndCompute(imageMat, new Mat(), keyPoints, descriptors);

                // 트래킹 타겟에 추가합니다.
                trackingTargets.Add(new TrackingTarget
                {
                    ReferenceTexture = imageReference,
                    ImageMat = imageMat,
                    KeyPoints = keyPoints,
                    Descriptors = descriptors
                });
            }
        }

        /// <summary>
        /// <para>프레임에서 타겟 결과를 반환합니다.</para>
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public void MatchFrame(Frame frame)
        {
            // 특징점 검출하고 계산
            MatOfKeyPoint frameKeyPoints = new MatOfKeyPoint();
            Mat frameDescriptors = new Mat();
            orb.detectAndCompute(frame.ImageMat, new Mat(), frameKeyPoints, frameDescriptors);

            trackingResults = new List<TrackingResult>(); // 결과 초기화
            foreach (var trackingTarget in trackingTargets)
            {
                // 트래킹 타겟과 프레임을 매칭합니다.
                MatOfDMatch matches = new MatOfDMatch();
                matcher.match(trackingTarget.Descriptors, frameDescriptors, matches);

                // 매칭 결과를 계산합니다.
                List<DMatch> matchesList = matches.toList();
                List<DMatch> goodMatchesList = new List<DMatch>();

                foreach (var match in matchesList)
                {
                    if (match.distance < 50)
                    {
                        goodMatchesList.Add(match);
                    }
                }

                // 매칭 결과가 4개 미만이면 다음으로 넘어갑니다.
                if (goodMatchesList.Count < 4)
                {
                    continue;
                }

                List<Point> srcPoints = new List<Point>();
                List<Point> dstPoints = new List<Point>();

                var imageKeyPointsArr = trackingTarget.KeyPoints.toArray();
                var frameKeyPointsArr = frameKeyPoints.toArray();
                foreach (var obj in goodMatchesList)
                {
                    srcPoints.Add(imageKeyPointsArr[obj.queryIdx].pt);
                    dstPoints.Add(frameKeyPointsArr[obj.trainIdx].pt);
                }

                MatOfPoint2f srcPointsMat = new MatOfPoint2f();
                srcPointsMat.fromList(srcPoints);
                MatOfPoint2f dstPointsMat = new MatOfPoint2f();
                dstPointsMat.fromList(dstPoints);

                // 호모그래피 행렬을 계산합니다.
                Mat homography = Calib3d.findHomography(srcPointsMat, dstPointsMat, Calib3d.RANSAC, 5.0);

                if (homography.empty())
                {
                    continue;
                }

                // 카메라 행렬
                float focalLength = 1.0f;
                Vector2 center = new Vector2(frame.ImageMat.width() / 2, frame.ImageMat.height() / 2);

                Mat cameraMatrix = new Mat(3, 3, CvType.CV_64FC1);
                cameraMatrix.put(0, 0, focalLength);
                cameraMatrix.put(0, 2, center.x);
                cameraMatrix.put(1, 1, focalLength);
                cameraMatrix.put(1, 2, center.y);
                cameraMatrix.put(2, 2, 1.0);

                // 호모그래피 행렬을 회전, 이동 및 법선 벡터로 분해합니다.
                List<Mat> rotations = new List<Mat>();
                List<Mat> translations = new List<Mat>();
                List<Mat> normals = new List<Mat>();

                // Decompose the homography matrix into rotation, translation, and normal vector.
                Calib3d.decomposeHomographyMat(homography, cameraMatrix, rotations, translations, normals);

                // 첫 번째 가능한 회전 및 이동 벡터를 선택합니다.
                Mat rotation = rotations[0];
                Mat translation = translations[0];

                // 회전 행렬을 오일러 각도로 변환합니다.
                Mat rvec = new Mat();
                Calib3d.Rodrigues(rotation, rvec);

                Vector3 eulerRotation = new Vector3(
                    (float)(rvec.get(0, 0)[0] * 180.0 / Math.PI),
                    (float)(rvec.get(1, 0)[0] * 180.0 / Math.PI),
                    (float)(rvec.get(2, 0)[0] * 180.0 / Math.PI)
                );

                Vector3 translationVector = new Vector3(
                    (float)translation.get(0, 0)[0],
                    (float)translation.get(1, 0)[0],
                    (float)translation.get(2, 0)[0]
                );

                // 트래킹 결과를 저장합니다.
                TrackingResult trackingResult = new TrackingResult
                {
                    MatchList = matchesList,
                    GoodMatchList = goodMatchesList,
                    TrackingTarget = trackingTarget,
                    EulerRotation = eulerRotation,
                    Translation = translationVector
                };
                trackingResults.Add(trackingResult);
            }
        }
    }
}