using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.ItemView
{
    public class CountEditableItemView<T> : CountableItemView<Game.Item.Inventory.InventoryItem>
        where T : Model.Inventory.Item
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        [SerializeField] public Button closeButton;
        [SerializeField] public Image closeImage;
        [SerializeField] public Button editButton;
        [SerializeField] public Image editImage;
        [SerializeField] public Text editText;

        private Model.CountEditableItem<T> _data;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            if (ReferenceEquals(closeButton, null) ||
                ReferenceEquals(closeImage, null) ||
                ReferenceEquals(editButton, null) ||
                ReferenceEquals(editImage, null) ||
                ReferenceEquals(editText, null))
            {
                throw new SerializeFieldNullException();
            }
        }

        #endregion

        public void SetData(Model.CountEditableItem<T> data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }

            _disposables.ForEach(d => d.Dispose());

            _data = data;
            _data.Item.Subscribe(SetItem).AddTo(_disposables);
            _data.Count.Subscribe(count => { Count = count; }).AddTo(_disposables);
            _data.EditButtonText.Subscribe(text => { editText.text = text; }).AddTo(_disposables);

            closeButton.OnClickAsObservable()
                .Subscribe(OnClickCloseButton)
                .AddTo(_disposables);

            editButton.OnClickAsObservable()
                .Subscribe(OnClickEditButton)
                .AddTo(_disposables);

            SetItem(data.Item.Value);
        }

        public override void Clear()
        {
            base.Clear();

            _disposables.ForEach(d => d.Dispose());

            _data = null;

            closeImage.enabled = false;
            editImage.enabled = false;
            editText.enabled = false;
        }

        private void SetItem(T item)
        {
            base.SetData(item, _data.Count.Value);

            closeImage.enabled = true;
            editImage.enabled = true;
            editText.enabled = true;
        }

        private void OnClickCloseButton(Unit u)
        {
            _data?.OnClose.OnNext(_data);
        }

        private void OnClickEditButton(Unit u)
        {
            _data?.OnEdit.OnNext(_data);
        }
    }
}
