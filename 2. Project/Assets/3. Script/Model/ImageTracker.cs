using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections.Generic;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace ImageTracking.Model
{
    /// <summary>
    /// <para>트래킹 결과 데이터 입니다.</para>
    /// </summary>
    [Serializable]
    public class TrackingResult
    {
        public bool IsTracking = false;

        public List<DMatch> MatchList;
        public List<DMatch> GoodMatchList;

        public Vector3 EulerRotation;
        public Vector3 Translation;

        public int TotalMatches => MatchList.Count;
        public int GoodMatches => GoodMatchList.Count;
        public float MatchRatio => (float)GoodMatches / TotalMatches;
    }

    /// <summary>
    /// <para>트래킹할 이미지 데이터 입니다.</para>
    /// </summary>
    [Serializable]
    public class TrackingTarget: IDisposable
    {
        public Texture2D ReferenceTexture;
        public Vector2 RealImageSize;

        [NonSerialized] public Mat ImageMat;
        [NonSerialized] public MatOfKeyPoint KeyPoints;
        [NonSerialized] public Mat Descriptors;

        [NonSerialized] public List<MatOfDouble> Corners;

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

        public void Initialize(ORB orb)
        {
            KeyPoints = new MatOfKeyPoint();
            Descriptors = new Mat();
            ImageMat = new Mat(ReferenceTexture.height, ReferenceTexture.width, CvType.CV_8UC4);
            Utils.texture2DToMat(ReferenceTexture, ImageMat);

            // RGB -> GRAY로 변환
            Imgproc.cvtColor(ImageMat, ImageMat, Imgproc.COLOR_RGBA2GRAY);

            // 특징점 검출하고 계산
            orb.detectAndCompute(ImageMat, new Mat(), KeyPoints, Descriptors);
        }

        public TrackingResult Match(DescriptorMatcher matcher, MatOfKeyPoint frameKeyPoints, Mat frameDescriptors, Mat cameraMatrix)
        {
            var trackingResult = new TrackingResult();

            // 매칭
            MatOfDMatch matches = new MatOfDMatch();
            matcher.match(Descriptors, frameDescriptors, matches);

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

            trackingResult.MatchList = matchesList;
            trackingResult.GoodMatchList = goodMatches;

            // 매칭 결과가 4개 미만이면 다음으로 넘어갑니다. (최소 기준)
            if (goodMatches.Count < 4)
            {
                return trackingResult;
            }

            // 
            if (trackingResult.MatchRatio < 0.1f)
            {
                return trackingResult;
            }

            // homography 계산
            List<Point> srcPoints = new List<Point>();
            List<Point> dstPoints = new List<Point>();
            foreach (var match in goodMatches)
            {
                srcPoints.Add(KeyPoints.toArray()[match.queryIdx].pt);
                dstPoints.Add(frameKeyPoints.toArray()[match.trainIdx].pt);
            }

            MatOfPoint2f srcPointsMat = new MatOfPoint2f();
            srcPointsMat.fromList(srcPoints);
            MatOfPoint2f dstPointsMat = new MatOfPoint2f();
            dstPointsMat.fromList(dstPoints);

            Mat homography = Calib3d.findHomography(srcPointsMat, dstPointsMat, Calib3d.RANSAC, 5);

            // homography가 없으면 다음으로 넘어갑니다.
            if (homography.empty())
            {
                return trackingResult;
            }

            // World Space what relative to the two camera
            Mat toWorldMat = cameraMatrix.inv() * homography;

            // 코너점 계산
            const double normalizedLength = 1.0;
            Corners = new List<MatOfDouble>
            {
                new MatOfDouble(0, 0, normalizedLength), // 왼쪽 상단
                new MatOfDouble(ReferenceTexture.width, 0, normalizedLength), // 오른쪽 상단
                new MatOfDouble(ReferenceTexture.width, ReferenceTexture.height, normalizedLength), // 오른쪽 하단
                new MatOfDouble(0, ReferenceTexture.height, normalizedLength) // 왼쪽 하단
            };

            // 코너점을 toWorldMat로 변환
            List<Vector3> localPoints = new List<Vector3>();
            foreach (var corner in Corners)
            {
                Mat resultPoint = toWorldMat * corner;
                // Vector3, Unity 좌표계로 변환
                Vector3 localPoint = new Vector3((float)resultPoint.get(0, 0)[0], -(float)resultPoint.get(1, 0)[0], (float)resultPoint.get(2, 0)[0]);
                localPoints.Add(localPoint);
            }

            // 벡터를 구함
            Vector3 right = localPoints[1] - localPoints[0];
            Vector3 up = localPoints[3] - localPoints[0];

            float width = right.magnitude;
            float height = up.magnitude;

            // 실제 이미지 크기로 변환
            float toRealSize = ((RealImageSize.x / width) +(RealImageSize.y / height)) / 2;
            Vector3 centerPoint = localPoints[0] + right * 0.5f + up * 0.5f;
            centerPoint *= toRealSize;

            // 벡터 정규화
            right.Normalize();
            up.Normalize();

            // 법선 벡터를 구함
            Vector3 forward = Vector3.Cross(up, right);

            Quaternion rotation = Quaternion.LookRotation(forward, up);
            Vector3 eurlerRotation = rotation.eulerAngles;

            trackingResult.EulerRotation = eurlerRotation;
            trackingResult.Translation = centerPoint;

            // 트래킹 성공
            trackingResult.IsTracking = true;
            return trackingResult;
        }
    }

    /// <summary>
    /// <para>프레임에 찾을 이미지들을 매칭하는 역할을 합니다.</para>
    /// </summary>
    public class ImageTracker : MonoBehaviour
    {
        [Header("Tracking Target")]
        public List<TrackingTarget> TrackingTargets = new List<TrackingTarget>();

        private ORB orb;
        private DescriptorMatcher matcher;

        public void Initialize()
        {
            // ORB 초기화
            orb = ORB.create();
            orb.setMaxFeatures(500);

            // Descriptor Matcher 초기화
            matcher = DescriptorMatcher.create(DescriptorMatcher.BRUTEFORCE_HAMMING);

            // 트래킹 타겟 초기화
            foreach (var trackingTarget in TrackingTargets)
            {
                trackingTarget.Initialize(orb);
            }
        }

        /// <summary>
        /// <para>프레임에서 타겟 결과를 반환합니다.</para>
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public List<TrackingResult> MatchFrame(Mat frame, float focalLength)
        {
            // 특징점 검출하고 계산
            MatOfKeyPoint frameKeyPoints = new MatOfKeyPoint();
            Mat frameDescriptors = new Mat();
            orb.detectAndCompute(frame, new Mat(), frameKeyPoints, frameDescriptors);

            // 카메라 행렬
            Mat cameraMatrix = new Mat(3, 3, CvType.CV_64FC1);
            cameraMatrix.put(0, 0, focalLength);
            cameraMatrix.put(1, 1, focalLength);
            cameraMatrix.put(0, 2, frame.width() / 2);
            cameraMatrix.put(1, 2, frame.height() / 2);
            cameraMatrix.put(2, 2, 1.0);

            var TrackingResults = new List<TrackingResult>();
            foreach (var trackingTarget in TrackingTargets)
            {
                TrackingResult trackingResult = trackingTarget.Match(matcher, frameKeyPoints, frameDescriptors, cameraMatrix);
                TrackingResults.Add(trackingResult);
            }

            return TrackingResults;
        }
    }
}