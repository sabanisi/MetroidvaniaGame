using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockCollisionDetector : MonoBehaviour
{
    [SerializeField] private Player _player;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        _player.AddDetectedBlock(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        _player.RemoveDetectedBlock(collision);
    }
}