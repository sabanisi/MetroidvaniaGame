using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : MonoBehaviour
{
    public Animator animator;
    [SerializeField] private Transform _transform;
    [SerializeField] private BoxCollider2D _collider;
    // [SerializeField] private SpriteRenderer _sprite;
    // [SerializeField] private Sprite _dieSprite1;
    // [SerializeField] private Sprite _dieSprite2;
    //[SerializeField] private DieEffect _dieEffectPrefab;
    
    private ButtonInputDetector _input;//入力を受け取るクラス
    //public bool _isUserControl { get; private set; }//ユーザーが操作しているかどうか
    private bool _isDied;
    private bool _isClear;
    private List<Block> _blockList; // blocks which will be perhaps collided
    private bool _isBeforeJump;//1フレーム前のジャンプキーの入力
    private bool _isJump;
    private bool _isJumpKeyDowning;

    private Vector3 _colliderHalfSize;
    private Vector3 _colliderOffset;
    private float _colliderUp, _colliderDown, _colliderRight, _colliderLeft;
    private Vector3 _pos; // position setting
    private Vector3 _prePos; // previous position setting
    private int _isHCollide; // 0: not, 1: right, -1: left
    private int _isVCollide; // 0: not, 1: up, -1: down
    private Block _blockHColliding; // the block which is touched horizontally
    private Block _blockVColliding; // the block which is touched vertically
    private const float COLLISION_ERROR = 1e-4f;

    // player parameters
    [SerializeField] private float _walkSpeed; // walking speed
    [SerializeField] private float _jumpSpeed; // initial speed to jump
    [SerializeField] private float _maxFallSpeed; // maximum falling speed
    [SerializeField] private float _minGravity; // minimum gravity
    [SerializeField] private float _initGravity; // initial gravity
    [SerializeField] private float _dGravity; // gravity rate of change
    private float _nowGravity; // now gravity
    private Vector3 _playerSpeed;
    [SerializeField] private float _maxJumpTime; // maximum time until jump is possible
    private float _jumpTime; // time until jump is possible for counting

    public void Start()
    {
        _blockList = new List<Block>();
        _input = GetComponent<ButtonInputDetector>();

        _colliderHalfSize = _collider.size / 2f;
        _colliderOffset = _collider.offset;
        _colliderUp = (_colliderHalfSize.y + _colliderOffset.y) * Mathf.Abs(_transform.localScale.y);
        _colliderDown = (_colliderHalfSize.y - _colliderOffset.y) * Mathf.Abs(_transform.localScale.y);
        _colliderRight = (_colliderHalfSize.x + _colliderOffset.x) * Mathf.Abs(_transform.localScale.x);
        _colliderLeft = (_colliderHalfSize.x - _colliderOffset.x) * Mathf.Abs(_transform.localScale.x);

        _isHCollide = 0;
        _isVCollide = 0;
        _blockHColliding = null;
        _blockVColliding = null;
        _playerSpeed = Vector3.zero;
        _pos = _transform.position;
        _prePos = _pos;
        _nowGravity = _initGravity;
        _jumpTime = 0f;
        _isBeforeJump = false;
        _isJump = false;
    }
    
    public void Update()
    {
        if (_isDied) return;
        //if (_gm._isStop) return;

        InitializeParameters();
        Walk();
        Jump();
        UpdatePos();
        BlockCollision();
        ApplyPos();
    }

    private void InitializeParameters()
    {
        _pos = _transform.position;
        _prePos = _pos;
        // on the ground
        if (_isVCollide < 0)
        {
            _nowGravity = _initGravity;
            _jumpTime = 0f;
            _playerSpeed.y = 0f;
            _isJump = false;
        }
    }

    private void UpdatePos()
    {
        // based on speed
        ApplyGravity();
        _pos += _playerSpeed * Time.deltaTime;
        // follow to the colliding block
        FollowBlock();
    }

    private void ApplyPos()
    {
        _transform.position = _pos;
    }

    private void Walk()
    {
        if (_input.IsLeft) // left
        {
            _playerSpeed.x = -_walkSpeed;
        }
        else if (_input.IsRight) // right
        {
            _playerSpeed.x = _walkSpeed;
        }
        else
        {
            _playerSpeed.x = 0f;
        }
    }

    private void Jump()
    {
        // add initial speed of jumping
        if (_isVCollide < 0 && !_isJump && !_isBeforeJump && _input.IsJump)
        {
            _playerSpeed.y = _jumpSpeed;
            _isJump = true;
            //SoundManager.PlaySE(SE_Enum.JUMP);
        }

        if (_isJump && _jumpTime < _maxJumpTime && _input.IsJump)
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

    private void ApplyGravity()
    {
        // collide with ceiling
        if (_isVCollide > 0)
        {
            if (_playerSpeed.y > 0) { _playerSpeed.y = 0f; }
            _jumpTime = _maxJumpTime;
            _isJump = false;
        }

        // apply gravity
        if (_isVCollide >= 0 && !_isJump)
        {
            _nowGravity = _nowGravity > _minGravity ? _nowGravity - _dGravity * Time.deltaTime : _minGravity;
            _playerSpeed.y = _playerSpeed.y > _maxFallSpeed ? _playerSpeed.y - _nowGravity * Time.deltaTime : _maxFallSpeed;
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

    private void BlockCollision()
    {
        if (_blockList.Count == 0)
        {
            _isHCollide = 0;
            _isVCollide = 0;
            return;
        }
        bool isUpdateH = false, isUpdateV = false;

        // block
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
                        if (_isHCollide < 0) { Die(); return; } // crushed
                        _pos.x = otherPos.x - otherSize.x - _colliderHalfSize.x;
                        _isHCollide = 1;
                        _blockHColliding = block;
                        isUpdateH = true;
                    }
                    else if ((otherPrePos.x+otherSize.x) - (_prePos.x-_colliderHalfSize.x) < COLLISION_ERROR
                            && (_pos.x-_colliderHalfSize.x) - (otherPos.x+otherSize.x) < COLLISION_ERROR) // left
                    {
                        if (_isHCollide > 0) { Die(); return; } // crushed
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
                    if (_isVCollide < 0) { Die(); return; } // crushed
                    _pos.y = otherPos.y - otherSize.y - _colliderUp;
                    _isVCollide = 1;
                    _blockVColliding = block;
                    isUpdateV = true;
                }
                else if ((otherPrePos.y+otherSize.y) - (_prePos.y-_colliderDown) < COLLISION_ERROR
                        && (_pos.y-_colliderDown) - (otherPos.y+otherSize.y) < COLLISION_ERROR) // down
                {
                    if (_isVCollide > 0) { Die(); return; } // crushed
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

    private void Die()
    {
        if (_isDied) return;
        // if (_gm._isStop) return;
        // SoundManager.PlaySE(SE_Enum.DIE);
        _collider.enabled = false;
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
            _blockList.Add(other.GetComponent<Block>());
        }
    }

    public void RemoveDetectedBlock(Collider2D other)
    {
        if (_isDied) return;
        
        if (other.tag.Equals("Block"))
        {
            foreach (Block block in _blockList)
            {
                if (block.gameObject.Equals(other.gameObject))
                {
                    _blockList.Remove(block);
                    break;
                }
            }
        }
    }
}
