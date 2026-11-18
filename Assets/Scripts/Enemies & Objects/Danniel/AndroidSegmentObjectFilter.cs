using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class AndroidSegmentObjectFilter : MonoBehaviour
{
    [Tooltip("Objects to deactivate when this segment runs on an Android device.")]
    [SerializeField]
    private GameObject[] objectsToDisableOnAndroid;

    private void Awake()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            return;
        }

        if (objectsToDisableOnAndroid == null)
        {
            return;
        }

        foreach (GameObject target in objectsToDisableOnAndroid)
        {
            if (target == null || target == gameObject)
            {
                continue;
            }

            target.SetActive(false);
        }
    }
}
