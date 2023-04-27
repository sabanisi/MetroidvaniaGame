using UnityEngine;

namespace Entity.Player
{
    public class PlayerConstantInfo:MonoBehaviour
    {
        [Header("移動速度")][SerializeField] private float _walkSpeed;
        public float WalkSpeed => _walkSpeed;
        [Header("初期ジャンプ速度")][SerializeField] private float _jumpSpeed;
        public float JumpSpeed => _jumpSpeed;
        [Header("最大落下速度")] [SerializeField] private float _maxFallSpeed;
        public float MaxFallSpeed => _maxFallSpeed;
        [Header("最小重力")][SerializeField] private float _minGravity;
        public float MinGravity => _minGravity;
        [Header("初期重力")][SerializeField] private float _initGravity;
        public float InitGravity => _initGravity;
        [Header("重力変化度")][SerializeField] private float _dGravity;
        public float DGravity => _dGravity;
        [Header("最大ジャンプ時間")][SerializeField] private float _maxJumpTime;
        public float MaxJumpTime => _maxJumpTime;
        [Header("歩行加速時間")][SerializeField] private float _accelerationMs;
        public float AccelerationMs => _accelerationMs;
        [Header("歩行減速時間")][SerializeField] private float _decelerationMs;
        public float DecelerationMs => _decelerationMs;
    }
}