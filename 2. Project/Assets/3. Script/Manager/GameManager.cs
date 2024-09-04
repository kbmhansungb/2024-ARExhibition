using ImageTracking.Model;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("GameManager")]
    [SerializeField] private FrameProvider frameProvider;
    [SerializeField] private ImageFinder imageFinder;
    [SerializeField] private ImageTracker imageTracker;

    [SerializeField] private RawImage tempRawImage;

    private void Start()
    {
        Initialize();

        frameProvider.Connect();
        imageFinder.Initialize();
    }

    public void Initialize()
    {
        frameProvider.ProvideFrameDelegate += (provider) => {
            // 프레임 제공자로 부터 프레임을 받아서 이미지 매칭을 수행합니다.
            imageFinder.MatchFrame(provider.CurrentFrame);

            var trackingResults = imageFinder.TrackingResults;
            tempRawImage.texture = provider.CurrentFrame.InstanceTexture;

            // 가장 매칭률이 높은 이미지를 찾습니다.
            int bestIndex = -1;
            float bestMatchRatio = 0f;
            foreach (var trackingResult in trackingResults)
            {
                if (trackingResult.TotalMatches > 0 && trackingResult.MatchRatio > bestMatchRatio)
                {
                    bestMatchRatio = trackingResult.MatchRatio;
                    bestIndex = trackingResults.IndexOf(trackingResult);
                }
            }

            if (bestIndex == -1)
            {
                return;
            }

            // 가장 매칭률이 높은 이미지를 트래킹합니다.
            var bestTrackingResult = trackingResults[bestIndex];
            imageTracker.Track(provider.CurrentFrame, bestTrackingResult);
        };
    }
}
