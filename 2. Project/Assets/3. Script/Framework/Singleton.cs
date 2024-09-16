using UnityEngine;

public class Singleton<Type> : MonoBehaviour
    where Type : MonoBehaviour
{
    private static Type instance;
    public static Type Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<Type>();
                if (instance == null)
                {
                    Debug.LogError(typeof(Type) + " is null");
                }
            }
            return instance;
        }
    }
}
