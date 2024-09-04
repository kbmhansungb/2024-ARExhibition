using NUnit.Framework;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using UnityEngine;

namespace ImageTracking.Model
{
    /// <summary>
    /// <para>트래킹 대상으로 부터 호모지니언스를 만들고 대상을 추적합니다.</para>
    /// </summary>
    public class ImageTracker : MonoBehaviour
    {
        [Header("ImageTracker")]
        [SerializeField] private Vector3 eulerRotation = Vector3.zero;
        [SerializeField] private Vector3 translation = Vector3.zero;

        public void ResetTracker()
        {

        }

        public void Track(Frame targetFrame, TrackingResult trackingResult)
        {
        }
    }
}