using UnityEngine;

public class Starter : MonoBehaviour
{
    void Start()
    {
        GameManager.Instance.Initialize();
        DataManager.Instance.Initialize();
        NetworkManager.Instance.Initialize();
        ObjectManager.Instance.Initialize();
        UIManager.Instance.Initialize();
    }
}
