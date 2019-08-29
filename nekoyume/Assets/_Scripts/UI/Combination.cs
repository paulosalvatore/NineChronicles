using System;
using System.Collections.Generic;
using Nekoyume.Manager;
using Nekoyume.BlockChain;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Stage = Nekoyume.Game.Stage;
using Assets.SimpleLocalization;
using Nekoyume.Helper;
using Nekoyume.Data;

namespace Nekoyume.UI
{
    public class Combination : Widget
    {
        public Button switchEquipmentsButton;
        public Image switchEquipmentsButtonImage;
        public Text switchEquipmentsButtonText;
        public Button switchConsumableButton;
        public Image switchConsumableButtonImage;
        public Text switchConsumableButtonText;
        public Module.Inventory inventory;
        public Button recipeButton;
        public Image recipeButtonImage;
        public Text recipeButtonText;
        public GameObject manualCombination;
        public Text materialsTitleText;
        public CombinationMaterialView equipmentMaterialView;
        public CombinationMaterialView[] materialViews;
        public GameObject materialViewsPlusImageContainer;
        public Button combinationButton;
        public Image combinationButtonImage;
        public Text combinationButtonText;
        public GameObject recipeCombination;
        public Button closeButton;
        public Button recipeCloseButton;
        public Recipe recipe;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        private Stage _stage;
        private Player _player;

        private SimpleItemCountPopup _simpleItemCountPopup;

        public Model.Combination Model { get; private set; }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();

            switchEquipmentsButtonText.text = LocalizationManager.Localize("UI_COMBINE_EQUIPMENTS");
            switchConsumableButtonText.text = LocalizationManager.Localize("UI_COMBINE_CONSUMABLES");
            recipeButtonText.text = LocalizationManager.Localize("UI_RECIPE");
            materialsTitleText.text = LocalizationManager.Localize("UI_COMBINATION_MATERIALS");
            combinationButtonText.text = LocalizationManager.Localize("UI_COMBINATION_ITEM");

            switchEquipmentsButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Model.consumablesOrEquipments.Value = UI.Model.Combination.ConsumablesOrEquipments.Equipments;
                })
                .AddTo(gameObject);
            switchConsumableButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Model.consumablesOrEquipments.Value = UI.Model.Combination.ConsumablesOrEquipments.Consumables;
                })
                .AddTo(gameObject);
            recipeButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();

                    Model.manualOrRecipe.Value =
                        Model.manualOrRecipe.Value == UI.Model.Combination.ManualOrRecipe.Manual ?
                            UI.Model.Combination.ManualOrRecipe.Recipe :
                            UI.Model.Combination.ManualOrRecipe.Manual;
                })
                .AddTo(gameObject);
            combinationButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    RequestCombination(Model);
                })
                .AddTo(gameObject);
            closeButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Close();
                })
                .AddTo(gameObject);
            recipeCloseButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Model.manualOrRecipe.Value = UI.Model.Combination.ManualOrRecipe.Manual;
                })
                .AddTo(gameObject);
            recipe.scrollerController.onClickCellView
                .Subscribe(cellView =>
                {
                    AudioController.PlayClick();
                    RequestCombination(cellView.Model);
                })
                .AddTo(gameObject);
        }

        #endregion

        public override void Show()
        {
            _simpleItemCountPopup = Find<SimpleItemCountPopup>();
            if (ReferenceEquals(_simpleItemCountPopup, null))
            {
                throw new NotFoundComponentException<SimpleItemCountPopup>();
            }

            base.Show();

            _stage = Game.Game.instance.stage;
            _stage.LoadBackground("combination");

            _player = _stage.GetPlayer();
            if (ReferenceEquals(_player, null))
            {
                throw new NotFoundComponentException<Player>();
            }

            _player.gameObject.SetActive(false);

            SetData(new Model.Combination(
                States.Instance.currentAvatarState.Value.inventory));

            AudioController.instance.PlayMusic(AudioController.MusicCode.Combination);
        }

        public override void Close()
        {
            Clear();

            _stage.GetPlayer(_stage.roomPosition);
            _stage.LoadBackground("room");
            _player.gameObject.SetActive(true);

            Find<Menu>()?.ShowRoom();

            base.Close();

            AudioController.instance.PlayMusic(AudioController.MusicCode.Main);
        }

        private void SetData(Model.Combination model)
        {
            if (ReferenceEquals(model, null))
            {
                Clear();
                return;
            }

            _disposablesForModel.DisposeAllAndClear();
            Model = model;
            Model.consumablesOrEquipments.Subscribe(Subscribe).AddTo(_disposablesForModel);
            Model.manualOrRecipe.Subscribe(Subscribe).AddTo(_disposablesForModel);
            Model.inventory.Value.selectedItemView.Subscribe(SubscribeInventorySelectedItem)
                .AddTo(_disposablesForModel);
            Model.itemCountPopup.Value.item.Subscribe(OnPopupItem).AddTo(_disposablesForModel);
            Model.itemCountPopup.Value.onClickCancel.Subscribe(OnClickClosePopup).AddTo(_disposablesForModel);
            Model.equipmentMaterial.Subscribe(equipmentMaterialView.SetData).AddTo(_disposablesForModel);
            Model.materials.ObserveAdd().Subscribe(OnAddStagedItems).AddTo(_disposablesForModel);
            Model.materials.ObserveRemove().Subscribe(OnRemoveStagedItems).AddTo(_disposablesForModel);
            Model.materials.ObserveReplace().Subscribe(_ => UpdateStagedItems()).AddTo(_disposablesForModel);
            Model.showMaterialsCount.Subscribe(SubscribeShowMaterialsCount).AddTo(_disposablesForModel);
            Model.readyForCombination.Subscribe(SetActiveCombinationButton).AddTo(_disposablesForModel);

            inventory.SetData(Model.inventory.Value);

            UpdateStagedItems();
        }

        private void Clear()
        {
            inventory.Clear();
            Model = null;
            _disposablesForModel.DisposeAllAndClear();

            foreach (var item in materialViews)
            {
                item.Clear();
            }
        }

        private void Subscribe(Model.Combination.ConsumablesOrEquipments value)
        {
            switch (value)
            {
                case UI.Model.Combination.ConsumablesOrEquipments.Consumables:
                    switchEquipmentsButtonImage.sprite = Resources.Load<Sprite>("UI/Textures/button_black_02");
                    switchConsumableButtonImage.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
                    equipmentMaterialView.gameObject.SetActive(false);
                    materialViewsPlusImageContainer.SetActive(false);
                    break;
                case UI.Model.Combination.ConsumablesOrEquipments.Equipments:
                    switchEquipmentsButtonImage.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
                    switchConsumableButtonImage.sprite = Resources.Load<Sprite>("UI/Textures/button_black_02");
                    equipmentMaterialView.gameObject.SetActive(true);
                    materialViewsPlusImageContainer.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
            
            inventory.Tooltip.Close();
        }
        
        private void Subscribe(Model.Combination.ManualOrRecipe value)
        {
            switch (value)
            {
                case UI.Model.Combination.ManualOrRecipe.Manual:
                    recipeButtonImage.sprite = Resources.Load<Sprite>("UI/Textures/button_black_02");
                    manualCombination.SetActive(true);
                    recipeCombination.SetActive(false);
                    break;
                case UI.Model.Combination.ManualOrRecipe.Recipe:
                    recipe.Reload(0);
                    recipeButtonImage.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
                    manualCombination.SetActive(false);
                    recipeCombination.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private void UpdateStagedItems(int startIndex = 0)
        {
            if (Model.consumablesOrEquipments.Value == UI.Model.Combination.ConsumablesOrEquipments.Equipments)
            {
                if (Model.equipmentMaterial.Value == null)
                {
                    equipmentMaterialView.Clear();    
                }
                else
                {
                    equipmentMaterialView.SetData(Model.equipmentMaterial.Value);
                }
            }
            
            var dataCount = Model.materials.Count;
            for (var i = startIndex; i < materialViews.Length; i++)
            {
                var item = materialViews[i];
                if (i < dataCount)
                {
                    item.SetData(Model.materials[i]);
                }
                else
                {
                    item.Clear();
                }
            }
        }

        private void SubscribeInventorySelectedItem(InventoryItemView view)
        {
            if (view is null)
            {
                return;
            }
            
            if (inventory.Tooltip.Model.target.Value == view.RectTransform)
            {
                inventory.Tooltip.Close();

                return;
            }

            inventory.Tooltip.Show(
                view.RectTransform,
                view.Model,
                value => !view.Model.dimmed.Value,
                LocalizationManager.Localize("UI_COMBINATION_REGISTER_MATERIAL"),
                tooltip =>
                {
                    Model.RegisterToStagedItems(tooltip.itemInformation.Model.item.Value);
                    inventory.Tooltip.Close();
                });
        }

        private void OnPopupItem(CountableItem data)
        {
            if (ReferenceEquals(data, null))
            {
                _simpleItemCountPopup.Close();
                return;
            }

            _simpleItemCountPopup.Pop(Model.itemCountPopup.Value);
        }

        private void OnClickClosePopup(Model.SimpleItemCountPopup data)
        {
            Model.itemCountPopup.Value.item.Value = null;
            _simpleItemCountPopup.Close();
        }

        private void OnAddStagedItems(CollectionAddEvent<CombinationMaterial> e)
        {
            if (e.Index >= materialViews.Length)
            {
                Model.materials.RemoveAt(e.Index);
                throw new AddOutOfSpecificRangeException<CollectionAddEvent<CountEditableItem>>(
                    materialViews.Length);
            }

            materialViews[e.Index].SetData(e.Value);
        }

        private void OnRemoveStagedItems(CollectionRemoveEvent<CombinationMaterial> e)
        {
            if (e.Index >= materialViews.Length)
            {
                return;
            }

            var dataCount = Model.materials.Count;
            for (var i = e.Index; i <= dataCount; i++)
            {
                var item = materialViews[i];

                if (i < dataCount)
                {
                    item.SetData(Model.materials[i]);
                }
                else
                {
                    item.Clear();
                }
            }
        }

        private void SubscribeShowMaterialsCount(int value)
        {
            for (var i = 0; i < materialViews.Length; i++)
            {
                materialViews[i].gameObject.SetActive(i < value);
            }
        }

        private void SetActiveCombinationButton(bool isActive)
        {
            if (isActive)
            {
                combinationButton.enabled = true;
                combinationButtonImage.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
                combinationButtonText.color = Color.white;
            }
            else
            {
                combinationButton.enabled = false;
                combinationButtonImage.sprite = Resources.Load<Sprite>("UI/Textures/button_gray_01");
                combinationButtonText.color = ColorHelper.HexToColorRGB("92A3B5");
            }
        }

        private void RequestCombination(Model.Combination data)
        {
            var materials = new List<CombinationMaterial>();
            if (data.equipmentMaterial.Value != null)
            {
                materials.Add(data.equipmentMaterial.Value);
            }

            materials.AddRange(data.materials);

            RequestCombination(materials);
        }

        private void RequestCombination(RecipeInfo info)
        {
            var materials = new List<CombinationMaterial>();
            foreach (var materialInfo in info.materialInfos)
            {
                if (materialInfo.id == 0) break;
                CombinationMaterial material = new CombinationMaterial(
                    Tables.instance.CreateItemBase(materialInfo.id), 1, 1, 1);
                materials.Add(material);
            }

            RequestCombination(materials);
        }

        private void RequestCombination(List<CombinationMaterial> materials)
        {
            ActionManager.instance.Combination(materials);
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickCombinationCombination);
            Find<CombinationLoadingScreen>().Show();
            Model.inventory.Value.RemoveItems(materials);
            Model.RemoveEquipmentMaterial();
            foreach (var material in materials)
            {
                States.Instance.currentAvatarState.Value.inventory.RemoveFungibleItem(material.item.Value.Data.id,
                    material.count.Value);
            }
            while (Model.materials.Count > 0)
            {
                Model.materials.RemoveAt(0);
            }

            // 에셋의 버그 때문에 스크롤 맨 끝 포지션으로 스크롤 포지션 설정 시 스크롤이 비정상적으로 표시되는 문제가 있음
            recipe.Reload(recipe.scrollerController.scroller.ScrollPosition - 0.1f);
        }
    }
}
