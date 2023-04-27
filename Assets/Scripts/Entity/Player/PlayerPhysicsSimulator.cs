using System;
using System.Collections.Generic;
using UnityEngine;

namespace Entity.Player
{
    public class PlayerPhysicsSimulator
    {
        //プレイヤーの定数情報
        private PlayerConstantInfo _constantInfo;

        //入力を検知するクラス
        private ButtonInputDetector _input;
        
        /// <summary>
        /// 死亡時の処理を代入する変数
        /// </summary>
        public Action DieListener;
        
        private List<Block> _blockList=new List<Block>(); // blocks which will be perhaps collided
        public List<Block> BlockList=> _blockList;
        
        private const float COLLISION_ERROR = 1e-4f;

        private bool _isBeforeJump;//1フレーム前のジャンプキーの入力
        private bool _isJump;
        
        private Vector3 _colliderHalfSize;
        private Vector3 _colliderOffset;
        private float _colliderUp, _colliderDown, _colliderRight, _colliderLeft;

        private Vector3 _pos;
        private Vector3 _prePos; // previous position setting
        private int _isHCollide; // 0: not, 1: right, -1: left
        private int _isVCollide; // 0: not, 1: up, -1: down
        private Block _blockHColliding; // the block which is touched horizontally
        private Block _blockVColliding; // the block which is touched vertically
        private float _jumpTime; // time until jump is possible for counting
        private float _nowGravity; // now gravity
        private Vector3 _playerSpeed;
        private float _pushingLeftTime;
        private float _pushingRightTime;


        public PlayerPhysicsSimulator(
            Vector2 colliderSize,
            Vector2 colliderOffset, 
            Vector3 localScale,
            PlayerConstantInfo constantInfo,
            ButtonInputDetector buttonInputDetector)
        {
            _constantInfo = constantInfo;
            _input = buttonInputDetector;
            
            _isBeforeJump = false;
            _isJump = false;
            
            _colliderHalfSize = colliderSize / 2f;
            _colliderOffset = colliderOffset;
            _colliderUp = (_colliderHalfSize.y + _colliderOffset.y) * Mathf.Abs(localScale.y);
            _colliderDown = (_colliderHalfSize.y - _colliderOffset.y) * Mathf.Abs(localScale.y);
            _colliderRight = (_colliderHalfSize.x + _colliderOffset.x) * Mathf.Abs(localScale.x);
            _colliderLeft = (_colliderHalfSize.x - _colliderOffset.x) * Mathf.Abs(localScale.x);
            
            _isHCollide = 0;
            _isVCollide = 0;
            _blockHColliding = null;
            _blockVColliding = null;
            _jumpTime = 0f;
            _nowGravity = 0f;
            _playerSpeed=Vector3.zero;
            
            _pushingLeftTime = 0f;
            _pushingRightTime = 0f;
        }
        
        /// <summary>
        /// 毎フレーム実行する処理
        /// </summary>
        /// <param name="prePos">シミュレート前の位置</param>
        /// <returns>シミュレート後の位置</returns>
        public Vector3 Update(Vector3 prePos)
        {
            InitializeParameters(prePos);
            Walk();
            Jump();
            UpdatePos();
            BlockCollision();
            return _pos;
        }
        
        private void InitializeParameters(Vector3 pos)
        {
            _pos = pos;
            _prePos = pos;
            // on the ground
            if (_isVCollide < 0)
            {
                _nowGravity = _constantInfo.InitGravity;
                _jumpTime = 0f;
                _playerSpeed.y = 0f;
                _isJump = false;
            }
        }
        
        private void Walk()
        {
            float accSec = Time.deltaTime / _constantInfo.AccelerationMs * 1000f;
            float decSec = Time.deltaTime / _constantInfo.DecelerationMs * 1000f;
            if (_input.IsLeft) // left
            {
                _pushingLeftTime += accSec;
                _pushingRightTime -= decSec;
            }
            else if (_input.IsRight) // right
            {
                _pushingRightTime += accSec;
                _pushingLeftTime -= decSec;
            }
            else
            {
                _pushingLeftTime -= decSec;
                _pushingRightTime -= decSec;
            }
            _pushingLeftTime = Mathf.Clamp01(_pushingLeftTime);
            _pushingRightTime = Mathf.Clamp01(_pushingRightTime);
            _playerSpeed.x = _constantInfo.WalkSpeed 
                             * (-WalkAccelerationRatio(_pushingLeftTime) 
                                + WalkAccelerationRatio(_pushingRightTime));
        }

        private float WalkAccelerationRatio(float sec)
        {
            return sec * sec;
        }
        
        private void Jump()
        {
            // add initial speed of jumping
            if (_isVCollide < 0 && !_isJump && !_isBeforeJump && _input.IsJump)
            {
                _playerSpeed.y = _constantInfo.JumpSpeed;
                _isJump = true;
            }

            if (_isJump && _jumpTime < _constantInfo.MaxJumpTime && _input.IsJump)
            {
                // count time until jump is possible
                _jumpTime += Time.deltaTime;
            }
            else
            {
                _isJump = false;
            }

            _isBeforeJump = _input.IsJump;
        }
        
        private void UpdatePos()
        {
            // based on speed
            ApplyGravity();
            _pos += _playerSpeed * Time.deltaTime;
            // follow to the colliding block
            FollowBlock();
        }
        
        private void BlockCollision()
    {
        if (_blockList.Count == 0)
        {
            _isHCollide = 0;
            _isVCollide = 0;
            return;
        }
        bool isUpdateH = false, isUpdateV = false;
        
        foreach (Block block in _blockList)
        {
            Vector3 otherPos = block.GetCurPos();
            Vector3 otherPrePos = block.GetPrePos();
            Vector3 otherSize = block.HalfSize;
            Vector3 otherSize2 = otherSize - new Vector3(COLLISION_ERROR, COLLISION_ERROR, 0f) / 2f;
            
            // horizontal collision
            if (block.BlockType == BLOCK_TYPE.BLOCK)
            {
                if (_pos.y+_colliderUp > otherPos.y-otherSize2.y
                    && _pos.y-_colliderDown < otherPos.y+otherSize2.y)
                {
                    if ((_prePos.x+_colliderHalfSize.x) - (otherPrePos.x-otherSize.x) < COLLISION_ERROR
                        && (otherPos.x-otherSize.x) - (_pos.x+_colliderHalfSize.x) < COLLISION_ERROR) // right
                    {
                        if (_isHCollide < 0) { DieListener(); return; } // crushed
                        _pos.x = otherPos.x - otherSize.x - _colliderHalfSize.x;
                        _isHCollide = 1;
                        _blockHColliding = block;
                        isUpdateH = true;
                    }
                    else if ((otherPrePos.x+otherSize.x) - (_prePos.x-_colliderHalfSize.x) < COLLISION_ERROR
                            && (_pos.x-_colliderHalfSize.x) - (otherPos.x+otherSize.x) < COLLISION_ERROR) // left
                    {
                        if (_isHCollide > 0) { DieListener(); return; } // crushed
                        _pos.x = otherPos.x + otherSize.x + _colliderHalfSize.x;
                        _isHCollide = -1;
                        _blockHColliding = block;
                        isUpdateH = true;
                    }
                }
            }

            // vertical collision
            if (_pos.x+_colliderHalfSize.x > otherPos.x-otherSize2.x
                && _pos.x-_colliderHalfSize.x < otherPos.x+otherSize2.x)
            {
                if ((_prePos.y+_colliderUp) - (otherPrePos.y-otherSize.y) < COLLISION_ERROR
                    && (otherPos.y-otherSize.y) - (_pos.y+_colliderUp) < COLLISION_ERROR
                    && block.BlockType == BLOCK_TYPE.BLOCK) // up
                {
                    if (_isVCollide < 0) { DieListener(); return; } // crushed
                    _pos.y = otherPos.y - otherSize.y - _colliderUp;
                    _isVCollide = 1;
                    _blockVColliding = block;
                    isUpdateV = true;
                }
                else if ((otherPrePos.y+otherSize.y) - (_prePos.y-_colliderDown) < COLLISION_ERROR
                        && (_pos.y-_colliderDown) - (otherPos.y+otherSize.y) < COLLISION_ERROR) // down
                {
                    if (_isVCollide > 0) { DieListener(); return; } // crushed
                    _pos.y = otherPos.y + otherSize.y + _colliderDown;
                    _isVCollide = -1;
                    _blockVColliding = block;
                    isUpdateV = true;
                }
            }
        }
        if (!isUpdateH)
        {
            _isHCollide = 0;
            _blockHColliding = null;
        }
        if (!isUpdateV)
        {
            _isVCollide = 0;
            _blockVColliding = null;
        }
    }
        
        private void ApplyGravity()
        {
            // collide with ceiling
            if (_isVCollide > 0)
            {
                if (_playerSpeed.y > 0) { _playerSpeed.y = 0f; }
                _jumpTime = _constantInfo.MaxJumpTime;
                _isJump = false;
            }

            // apply gravity
            if (_isVCollide >= 0 && !_isJump)
            {
                _nowGravity = _nowGravity > _constantInfo.MinGravity ? _nowGravity - _constantInfo.DGravity * Time.deltaTime : _constantInfo.MinGravity;
                _playerSpeed.y = _playerSpeed.y > _constantInfo.MaxFallSpeed ? _playerSpeed.y - _nowGravity * Time.deltaTime : _constantInfo.MaxFallSpeed;
            }
        }
        
        private void FollowBlock()
        {
            // lean to the block
            if (_blockHColliding != null)
            {
                Vector3 otherDisPos = _blockHColliding.DisPos;
                // pushed by the block
                if ((_isHCollide > 0 && otherDisPos.x < 0) || (_isHCollide < 0 && otherDisPos.x > 0))
                {
                    _pos.x += otherDisPos.x;
                }
            }
            // collide with the block vertically
            if (_blockVColliding != null)
            {
                Vector3 otherDisPos = _blockVColliding.DisPos;
                if (_isVCollide < 0) // ride on the block
                {
                    _pos += otherDisPos;
                }
                else if (_isVCollide > 0 && otherDisPos.y < 0) // collide from below
                {
                    _pos.y += otherDisPos.y;
                    _playerSpeed.y = otherDisPos.y/Time.deltaTime;
                }
            }
        }
    }
}