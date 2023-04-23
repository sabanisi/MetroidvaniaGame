using UnityEngine;

public class Anker : MonoBehaviour
{
    [SerializeField] private float _moveTakenMs;
    public float MoveTakenMs => _moveTakenMs;
}
