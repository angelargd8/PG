using UnityEngine;

[DisallowMultipleComponent]
public sealed class AlexThrowHands : MonoBehaviour
{
    [SerializeField] private Transform _leftAnchor;
    [SerializeField] private Transform _rightAnchor;
    [Tooltip("Nombre exacto para rigs no humanoides. No se buscan dedos.")]
    [SerializeField] private string _leftHandBoneName = "mixamorig:LeftHand";
    [SerializeField] private string _rightHandBoneName = "mixamorig:RightHand";
    [SerializeField] private Vector3 _leftLocalOffset;
    [SerializeField] private Vector3 _rightLocalOffset;

    public Transform LeftAnchor => _leftAnchor;
    public Transform RightAnchor => _rightAnchor;

    public bool ResolveAnchors()
    {
        if (_leftAnchor == null)
            _leftAnchor = CreateAnchor(HumanBodyBones.LeftHand, _leftHandBoneName,
                "LeftThrowAnchor", _leftLocalOffset);
        if (_rightAnchor == null)
            _rightAnchor = CreateAnchor(HumanBodyBones.RightHand, _rightHandBoneName,
                "RightThrowAnchor", _rightLocalOffset);
        if (_leftAnchor != null && _rightAnchor != null) return true;
        Debug.LogError("[AlexThrowHands] Asigna ambos anchors o los nombres exactos de los huesos de las manos.", this);
        return false;
    }

    private Transform CreateAnchor(HumanBodyBones bone, string boneName, string anchorName, Vector3 offset)
    {
        Transform hand = null;
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman) hand = animator.GetBoneTransform(bone);
        if (hand == null)
        {
            foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == boneName) { hand = candidate; break; }
            }
        }
        if (hand == null) return null;
        Transform existing = hand.Find(anchorName);
        if (existing != null) return existing;
        Transform anchor = new GameObject(anchorName).transform;
        anchor.SetParent(hand, false);
        anchor.localPosition = offset;
        return anchor;
    }
}
