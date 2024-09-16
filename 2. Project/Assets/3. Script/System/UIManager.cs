using UnityEngine;

public enum EUIState
{
    Sample,
    Camera,
}

public class UIManager : Singleton<UIManager>
{
    [Header("UIManager")]
    [SerializeField] private SamplePresenter samplePresenter;
    [SerializeField] private CameraPresenter cameraPresenter;

    private EUIState UIState;

    public void Initialize()
    {
        samplePresenter.Initialize();
        cameraPresenter.Initialize();

        SetState(EUIState.Camera);
    }

    public void SetState(EUIState uiType)
    {
        switch (uiType)
        {
            case EUIState.Sample:
                samplePresenter.gameObject.SetActive(true);
                cameraPresenter.gameObject.SetActive(false);
                break;
            case EUIState.Camera:
                samplePresenter.gameObject.SetActive(false);
                cameraPresenter.gameObject.SetActive(true);
                break;
        }
    }
}
