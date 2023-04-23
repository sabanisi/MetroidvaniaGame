using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Entity.Player
{
    public class PlayerModel
    {
        private readonly ReactiveProperty<Vector3> _pos;
        public IReadOnlyReactiveProperty<Vector3> Pos => _pos;

        private bool _isDied;
        private bool _isClear;

        //物理演算を行うクラス
        private PlayerPhysicsSimulator _physicsSimulator;

        public PlayerModel(Vector3 initPos,Vector2 colliderSize,Vector2 colliderOffset,Vector3 localScale,PlayerConstantInfo constantInfo,ButtonInputDetector buttonInputDetector)
        {
            _pos = new ReactiveProperty<Vector3>(initPos);
            _physicsSimulator = new PlayerPhysicsSimulator(
                colliderSize,
                colliderOffset,
                localScale,
                constantInfo,
                buttonInputDetector);

            _physicsSimulator.DieListener = Die;
            _isDied = false;
            _isClear = false;
        }

        /// <summary>
        /// 毎フレーム実行する処理
        /// </summary>
        public void Update()
        {
            if (_isDied) return;
            _pos.Value = _physicsSimulator.Update(_pos.Value);
        }
        
        private void Die()
        {
            if (_isDied) return;
            _isDied = true;
            Debug.Log("died");
        }
        
        /// <summary>
        /// プレイヤーの周囲のブロックを取得する
        /// </summary>
        /// <param name="other">プレイヤーの周囲のブロック</param>
        public void AddDetectedBlock(Collider2D other)
        {
            if (_isDied) return;

            if (other.tag.Equals("Block"))
            {
                _physicsSimulator.BlockList.Add(other.GetComponent<Block>());
            }
        }

        public void RemoveDetectedBlock(Collider2D other)
        {
            if (_isDied) return;
        
            if (other.tag.Equals("Block"))
            {
                foreach (Block block in _physicsSimulator.BlockList)
                {
                    if (block.gameObject.Equals(other.gameObject))
                    {
                        _physicsSimulator.BlockList.Remove(block);
                        break;
                    }
                }
            }
        }
    }
}