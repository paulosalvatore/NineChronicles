﻿using EnhancedUI.EnhancedScroller;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class InventoryScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
    {

        [SerializeField] public EnhancedScroller scroller;
        [SerializeField] public InventoryCellView cellViewPrefab;
        [SerializeField] public int numberOfInnerItemPerCell = 1;

        /// <summary>
        /// `_scroller.Delegate`를 할당할 때, `_scroller` 내부에서 `_reloadData = true`가 된다.
        /// 이때문에 `SetData()`를 통해 `_dataList`를 할당하기 전에 `GetNumberOfCells()` 등과 같은
        /// `EnhancedScroller`의 `LifeCycle` 함수가 호출되면서 `null` 참조 문제가 발생한다.
        /// 그 상황을 피하기 위해서 빈 리스트를 할당한다.
        /// </summary>
        private ReactiveCollection<Model.Inventory.Item> _dataList = new ReactiveCollection<Model.Inventory.Item>();

        private float _cellViewHeight = 100f;

        #region Mono

        private void Awake()
        {
            if (ReferenceEquals(scroller, null) ||
                ReferenceEquals(cellViewPrefab, null))
            {
                throw new SerializeFieldNullException();
            }

            scroller.Delegate = this;

            _cellViewHeight = cellViewPrefab.GetComponent<RectTransform>().rect.height;
        }

        #endregion

        #region IEnhancedScrollerDelegate

        public int GetNumberOfCells(EnhancedScroller scr)
        {
            return Mathf.CeilToInt((float) _dataList.Count / numberOfInnerItemPerCell);
        }

        public float GetCellViewSize(EnhancedScroller scr, int dataIndex)
        {
            return _cellViewHeight;
        }

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scr, int dataIndex, int cellIndex)
        {
            var cellView = scroller.GetCellView(cellViewPrefab) as InventoryCellView;
            if (ReferenceEquals(cellView, null))
            {
                throw new FailedToInstantiateGameObjectException(cellViewPrefab.name);
            }

            var di = dataIndex * numberOfInnerItemPerCell;

            cellView.name = $"Cell {di} to {di + numberOfInnerItemPerCell - 1}";
            cellView.SetData(_dataList, di);

            return cellView;
        }

        #endregion

        public void SetData(ReactiveCollection<Model.Inventory.Item> dataList)
        {
            if (ReferenceEquals(dataList, null))
            {
                dataList = new ReactiveCollection<Model.Inventory.Item>();
            }

            _dataList = dataList;
            scroller.ReloadData();
        }

//        public void SetItemToCoveredAndDimmed(int id, bool isCover, bool isDim)
//        {
//            for (int i = _dataList.Count - 1; i >= 0; i--)
//            {
//                var d = _dataList[i];
//                if (d.Item.Data.Id == id)
//                {
//                    d.Covered.Value = isCover;
//                    d.Dimmed.Value = isDim;
//                    break;
//                }
//            }
//        }
    }
}
