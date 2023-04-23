using Unity.VisualScripting;
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
    private SimpleObjectMover _parent;

    void Start()
    {
        Vector3 scale = _transform.localScale;
        HalfSize = new Vector3(_collider.size.x * Mathf.Abs(scale.x) / 2f,
                                _collider.size.y * Mathf.Abs(scale.y) / 2f,
                                0);
        Transform parentTf = _transform.root;
        if (!parentTf.Equals(_transform))
        {
            _parent = parentTf.GetComponent<SimpleObjectMover>();
        }
    }

    public Vector3 GetCurPos() { return _transform.position; }

    public Vector3 GetPrePos() { return _transform.position - DisPos; }

    public Vector3 DisPos => _parent == null ? Vector3.zero : _parent.DisPos; 
}
