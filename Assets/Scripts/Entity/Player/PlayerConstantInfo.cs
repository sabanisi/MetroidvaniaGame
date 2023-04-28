using UnityEngine;

namespace Entity.Player
{
    public class PlayerConstantInfo:MonoBehaviour
    {
        [Header("移動速度")][SerializeField] private float _walkSpeed;
        public float WalkSpeed => _walkSpeed;
        [Header("最大ジャンプ高さ")][SerializeField] private float _maxJumpHeight;
        public float MaxJumpHeight => _maxJumpHeight;
        [Header("最大ジャンプ時間(ms)")][SerializeField] private float _maxJumpMs;
        public float MaxJumpMs => _maxJumpMs;
        [Header("歩行加速時間(ms)")][SerializeField] private float _accelerationMs;
        public float AccelerationMs => _accelerationMs;
        [Header("歩行減速時間(ms)")][SerializeField] private float _decelerationMs;
        public float DecelerationMs => _decelerationMs;
        [Header("コヨーテ時間(ms)")][SerializeField] private float _maxCoyoteMs;
        public float MaxCoyoteMs => _maxCoyoteMs;
        [Header("着地前ジャンプ入力受付時間(ms)")][SerializeField] private float _maxExtraJumpMs;
        public float MaxExtraJumpMs => _maxExtraJumpMs;
        [Header("空中で可能なジャンプ回数")][SerializeField] private int _maxAirJumpCount;
        public int MaxAirJumpCount => _maxAirJumpCount;
    }
}