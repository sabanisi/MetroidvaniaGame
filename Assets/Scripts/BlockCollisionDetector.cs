using Entity.Player;
using UnityEngine;

public class BlockCollisionDetector : MonoBehaviour
{
    [SerializeField] private PlayerPresenter _player;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        _player.AddDetectedBlockListener(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        _player.RemoveDetectedBlockListener(collision);
    }
}