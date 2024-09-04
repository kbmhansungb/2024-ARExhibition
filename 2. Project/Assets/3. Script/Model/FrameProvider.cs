using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.VideoioModule;
using System;
using System.Threading;
using UnityEngine;

namespace ImageTracking.Model
{
    /// <summary>
    /// <para>프레임 정의 입니다.</para>
    /// </summary>
    [Serializable]
    public class Frame: IDisposable
    {
        public Texture2D InstanceTexture;
        public Mat ImageMat;

        public void Dispose()
        {
            if (InstanceTexture != null)
            {
                UnityEngine.Object.Destroy(InstanceTexture);
                InstanceTexture = null;
            }

            if (ImageMat != null)
            {
                ImageMat.Dispose();
                ImageMat = null;
            }
        }
    }

    /// <summary>
    /// <para><see cref="IFrameProvider"/>의 델리게이트 입니다.</para>
    /// </summary>
    /// <param name="Val"></param>
    public delegate void FrameProviderDelegate(IFrameProvider Val);

    /// <summary>
    /// <para>프레임 제공하는 역할을 합니다.</para>
    /// </summary>
    public interface IFrameProvider
    {
        /// <summary>
        /// <para>프레임이 업데이트 되면 콜백을 호출합니다.</para>
        /// </summary>
        public FrameProviderDelegate ProvideFrameDelegate { get; set; }

        /// <summary>
        /// <para>마지막 프레임 입니다.</para>
        /// </summary>
        public Frame CurrentFrame { get; }
    }

    /// <summary>
    /// <para>카메라를 읽어 프레임을 제공합니다.</para>
    /// </summary>
    public class FrameProvider : MonoBehaviour, IFrameProvider
    {
        private VideoCapture videoCapture = null;
        private bool isConnected = false;

        private FrameProviderDelegate provideFrameDelegate = new FrameProviderDelegate((_)=> { });
        private Frame currentFrame = null;

        #region IFrameProvider

        public FrameProviderDelegate ProvideFrameDelegate { get => provideFrameDelegate; set => provideFrameDelegate = value; }
        public Frame CurrentFrame => currentFrame;

        #endregion

        public void Connect()
        {
            videoCapture = new VideoCapture(0);
            isConnected = true;
        }

        public void Disconnect()
        {
            videoCapture?.release();
            videoCapture = null;

            isConnected = false;
        }

        public void OnDestroy()
        {
            Disconnect();
        }

        public void Update()
        {
            if (!isConnected)
                return;

            if (videoCapture.isOpened())
            {
                Mat frame = new Mat();
                videoCapture.read(frame);

                if (!frame.empty())
                {
                    // BRG -> RGB로 변환
                    Imgproc.cvtColor(frame, frame, Imgproc.COLOR_BGR2RGB);

                    // texture 생성
                    Texture2D texture = new Texture2D(frame.cols(), frame.rows(), TextureFormat.RGBA32, false);
                    Utils.matToTexture2D(frame, texture);

                    // 프레임 업데이트
                    currentFrame?.Dispose();
                    currentFrame = new Frame()
                    {
                        InstanceTexture = texture,
                        ImageMat = frame
                    };

                    // 콜백 호출
                    provideFrameDelegate.Invoke(this);
                }
            }
        }
    }
}