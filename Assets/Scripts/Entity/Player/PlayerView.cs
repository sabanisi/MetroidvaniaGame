using UnityEngine;

namespace Entity.Player
{
    public class PlayerView:MonoBehaviour
    {
        [SerializeField] private Transform _transform;
        [SerializeField] private BoxCollider2D _collider;
        public BoxCollider2D Collider => _collider;

        /// <summary>
        /// プレイヤーの位置を変更する
        /// </summary>
        public void OnPosChanged(Vector3 pos)
        {
            _transform.position = pos;
        }
    }
}