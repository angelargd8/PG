using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyEnemyCue : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer _arrowRenderer;

    [Header("Hand Colors")]
    [SerializeField] private Color _anyColor = new Color(0.5f, 0f, 1f);
    [SerializeField] private Color _rightColor = Color.red;
    [SerializeField] private Color _leftColor = Color.blue;


    public void Configure(
        JeremyCutDirection direction,
        JeremyHand hand)
    {
        SetDirection(direction);
        SetHandColor(hand);
    }


    private void SetDirection(JeremyCutDirection direction)
    {
        float angle = direction switch
        {
            JeremyCutDirection.Up => 0f,
            JeremyCutDirection.Right => -90f,
            JeremyCutDirection.Down => 180f,
            JeremyCutDirection.Left => 90f,
            _ => 0f
        };

        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }


    private void SetHandColor(JeremyHand hand)
    {
        if (_arrowRenderer == null)
        {
            return;
        }

        _arrowRenderer.color = hand switch
        {
            JeremyHand.Left => _leftColor,
            JeremyHand.Right => _rightColor,
            _ => _anyColor
        };
    }
}