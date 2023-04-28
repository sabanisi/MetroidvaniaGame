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

        private Vector3 _colliderHalfSize;
        private Vector3 _colliderOffset;
        private float _colliderUp, _colliderDown, _colliderRight, _colliderLeft;

        private Vector3 _pos;
        private Vector3 _prePos; // previous position setting
        
        // for collision with block
        private int _isHCollide; // 0: not, 1: right, -1: left
        private int _isVCollide; // 0: not, 1: up, -1: down
        private Block _blockHColliding; // the block which is touched horizontally
        private Block _blockVColliding; // the block which is touched vertically

        // for walk
        private float _pushingLeftTime;
        private float _pushingRightTime;
        
        // for jump
        private float _jumpTime; // time until jump is possible for counting
        private bool _isJump;
        private bool _isConsumeGroundJump;
        private int _airJumpCount; // for counting a series of jumping until landing
        private float _coyoteTime; // for counting jump key input possible time after falling from the cliff
        private float _extraJumpTime; // for counting jump key input possible time before landing
        private bool _jumpInputBuffer;
        private Vector3 _inertia;

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
            
            _pushingLeftTime = 0f;
            _pushingRightTime = 0f;
            
            _jumpTime = .5f;
            _isJump = false;
            _isConsumeGroundJump = false;
            _airJumpCount = _constantInfo.MaxAirJumpCount; // cannot jump at the first
            _coyoteTime = 0f;
            _extraJumpTime = 0f;
            _jumpInputBuffer = false;
            _inertia = Vector3.zero;
        }
        
        /// <summary>
        /// 毎フレーム実行する処理
        /// </summary>
        /// <param name="prePos">シミュレート前の位置</param>
        /// <returns>シミュレート後の位置</returns>
        public Vector3 Update(Vector3 prePos)
        {
            _pos = prePos;
            _prePos = prePos;
            Walk();
            JumpAndFall();
            FollowBlock();
            BlockCollision();
            _isBeforeJump = _input.IsJump;
            return _pos;
        }

        private void Walk()
        {
            float accSec = Time.deltaTime / _constantInfo.AccelerationMs * 1000f;
            float decSec = Time.deltaTime / _constantInfo.DecelerationMs * 1000f;
            if (_input.IsLeft)
            {
                _pushingLeftTime += accSec;
                _pushingRightTime -= decSec;
            }
            else if (_input.IsRight)
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
            float accRatio = _pushingRightTime*_pushingRightTime - _pushingLeftTime*_pushingLeftTime;
            _pos.x += _constantInfo.WalkSpeed * accRatio * Time.deltaTime;
        }

        private void JumpAndFall()
        {
            // buffer jump key input
            _extraJumpTime += Time.deltaTime;
            if (_input.IsJump && !_isBeforeJump)
            {
                _jumpInputBuffer = true;
                _extraJumpTime = 0f;
            }
            if (_extraJumpTime > _constantInfo.MaxExtraJumpMs * .001f)
            {
                _jumpInputBuffer = false;
                _extraJumpTime = 0f;
            }
            
            // inertia
            if (!_isConsumeGroundJump && _blockVColliding != null && _isVCollide < 0)
            {
                _inertia = _blockVColliding.DisPos;
            }

            // one the ground
            if (_isVCollide < 0)
            {
                _jumpTime = .5f;
                _coyoteTime = 0f;
                _isJump = false;
                _isConsumeGroundJump = false;
                _airJumpCount = 0;
                // ground jump
                if (/*_input.IsJump && !_isBeforeJump*/_jumpInputBuffer)
                {
                    _jumpTime = 0f;
                    _isJump = true;
                    _isConsumeGroundJump = true;
                    Debug.Log("ground jump");
                }
                else { return; }
            }

            // collide with the ceiling while ascending
            if (_isVCollide > 0 && _jumpTime < .5f)
            {
                _jumpTime = .5f;
                _isJump = false;
            }
            
            // in midair
            if (_isVCollide == 0)
            {
                if (_coyoteTime < _constantInfo.MaxCoyoteMs * .001f)
                { // coyote jump
                    if (_input.IsJump && !_isBeforeJump && !_isConsumeGroundJump)
                    {
                        _jumpTime = 0f;
                        _isJump = true;
                        _isConsumeGroundJump = true;
                        Debug.Log("coyote jump");
                    }
                    _coyoteTime += Time.deltaTime;
                }
                else
                {
                    if (_input.IsJump && !_isBeforeJump && _airJumpCount < _constantInfo.MaxAirJumpCount)
                    { // air jump
                        _jumpTime = 0f;
                        _isJump = true;
                        _airJumpCount++;
                        Debug.Log("air jump: " + _airJumpCount);
                    }
                }
                // release or not press jump key
                if (_isJump && !_input.IsJump) { _isJump = false; }
                if (!_isJump && _jumpTime < .35f) { _jumpTime = .35f; }
            }
            
            // move vertically; jump or fall
            float dTime = Time.deltaTime / _constantInfo.MaxJumpMs * 1000f;
            _jumpTime += dTime;
            _jumpTime = Mathf.Clamp01(_jumpTime);
            //_pos.y += Mathf.Cos(Mathf.PI * _jumpTime) * Mathf.PI * _constantInfo.MaxJumpHeight * dTime;
            _pos.y += 8f * (.5f - _jumpTime) * dTime * _constantInfo.MaxJumpHeight;
                      //* (1f - (float)_airJumpCount/(float)(_constantInfo.MaxAirJumpCount+1));
            //if (_isConsumeGroundJump && _airJumpCount <= 0) { _pos += _inertia; }
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
                }
            }
        }
    }
}