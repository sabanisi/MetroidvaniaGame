using UnityEngine;

public class Block : MonoBehaviour
{
    [SerializeField] private Transform _transform;
    [SerializeField] private BoxCollider2D _collider;
    [SerializeField] private BLOCK_TYPE _blockType;
    public BLOCK_TYPE BlockType // block or platform
    {
        get { return _blockType; }
        private set { _blockType = value; }
    }
    public Vector3 HalfSize { get; private set; } // half boxcollider size
    private Vector3 _prePos; // position of 1 frame before
    public Vector3 DisPos { get; private set; } // displacement at 1 frame

    void Start()
    {
        Vector3 scale = _transform.localScale;
        HalfSize = new Vector3(_collider.size.x * Mathf.Abs(scale.x) / 2f,
                                _collider.size.y * Mathf.Abs(scale.y) / 2f,
                                0);
        _prePos = _transform.position;
    }

    void Update()
    {
        Vector3 curPos = _transform.position;
        DisPos = curPos - _prePos;
        _prePos = curPos;
    }

    public Vector3 GetCurPos() { return _transform.position; }
    
    public Vector3 GetPrePos() { return _transform.position - DisPos; }
    //
    // public Vector3 GetDPos() { return _parent == null ? Vector3.zero : _parent.GetDPos(); }
    //
    // public Vector3 GetSize() { return _halfSize; }
    //
    // public void SetParent(ObjectMover parent) { _parent = _parent; }
}
