using UnityEngine;

public class PlayerFollow : MonoBehaviour
{
    [SerializeField] Transform _player;
    private void OnEnable()
    {
        transform.position = _player.position;
    }
}
