using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremySwordAttachment : MonoBehaviour, IExperienceRuntime
{
    public enum Hand
    {
        Left,
        Right
    }

    [Header("Settings")]
    [SerializeField] private Hand _hand;

    private Transform _originalParent;


    public void BeginExperience()
    {
        _originalParent = transform.parent;

        string anchorName = _hand == Hand.Left
            ? "LeftWeaponAnchor"
            : "RightWeaponAnchor";

        GameObject anchorObject = GameObject.Find(anchorName);

        if (anchorObject == null)
        {
            Debug.LogError($"[JeremySwordAttachment] No se encontró {anchorName}.", this);
            return;
        }

        transform.SetParent(anchorObject.transform, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }


    public void EndExperience()
    {
        if (_originalParent == null)
        {
            return;
        }

        transform.SetParent(_originalParent, false);
    }
}