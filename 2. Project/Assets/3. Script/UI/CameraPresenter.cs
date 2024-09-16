using UnityEngine;
using UnityEngine.UI;

public class CameraPresenter : MonoBehaviour
{
    [Header("CameraPresenter")]
    [SerializeField] private RawImage rawImage;
    [SerializeField] private Button openSampleButton;

    public void Initialize()
    {
        openSampleButton.onClick.AddListener(() =>
        {
            UIManager.Instance.SetState(EUIState.Sample);
        });
    }
}
