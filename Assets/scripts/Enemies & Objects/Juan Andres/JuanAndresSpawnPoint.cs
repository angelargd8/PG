using UnityEngine;

[DisallowMultipleComponent]
public sealed class JuanAndresSpawnPoint : MonoBehaviour
{
    private JuanAndresTarget _occupant;


    public bool IsAvailable =>
        _occupant == null;


    public bool TryReserve(JuanAndresTarget target)
    {
        if (target == null || !IsAvailable)
        {
            return false;
        }

        _occupant = target;

        return true;
    }


    public void Release(JuanAndresTarget target)
    {
        if (_occupant != target)
        {
            return;
        }

        _occupant = null;
    }
}