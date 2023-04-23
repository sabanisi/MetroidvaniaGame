using System;
using UniRx;
using UnityEditor.Build.Player;
using UnityEngine;

namespace Entity.Player
{
    public class PlayerPresenter:MonoBehaviour
    {
        [SerializeField] private ButtonInputDetector _buttonInputDetector;
        [SerializeField] private PlayerView _view;
        [SerializeField]private PlayerConstantInfo _constantInfo;
        private PlayerModel _model;

        public Action<Collider2D> AddDetectedBlockListener;
        public Action<Collider2D> RemoveDetectedBlockListener;
            
        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _model = new PlayerModel(
                transform.position,
                _view.Collider.size,
                _view.Collider.offset,
                transform.localScale,
                _constantInfo,
                _buttonInputDetector);
            Bind();
        }

        //ModelとViewを結びつける
        private void Bind()
        {
            _model.Pos.Subscribe(_view.OnPosChanged).AddTo(gameObject);
            AddDetectedBlockListener = _model.AddDetectedBlock;
            RemoveDetectedBlockListener = _model.RemoveDetectedBlock;
        }

        //TODO:GameManagerを用いた実装に変更する
        private void Update()
        {
            _model.Update();
        }
    }
}