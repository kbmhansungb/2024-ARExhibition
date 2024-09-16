using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SamplePresenter : MonoBehaviour
{
    [Header("SamplePresenter")]
    [SerializeField] private Image markerImage;
    [SerializeField] private Button closeButton;

    [Space(10)]
    [SerializeField] private TextMeshProUGUI imageNameText;
    [SerializeField] private Button beforeImageButton;
    [SerializeField] private Button nextImageButton;

    public void Initialize()
    {
        closeButton.onClick.AddListener(() =>
        {
            UIManager.Instance.SetState(EUIState.Camera);
        });
    }
}
