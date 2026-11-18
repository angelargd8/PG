using UnityEngine;

public class SegmentAnchors : MonoBehaviour
{
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    public Transform StartPoint => startPoint;
    public Transform EndPoint => endPoint;
}