using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EnhanceEquipment : EnhancementPanel<EnhancementMaterialView>
    {
        public Image arrowImage;
        public GameObject message;
        public TextMeshProUGUI messageText;

        public override bool IsSubmittable =>
            !(States.Instance.AgentState is null) &&
            States.Instance.GoldBalanceState.gold >= CostNCG &&
            !(States.Instance.CurrentAvatarState is null) &&
            States.Instance.CurrentAvatarState.actionPoint >= CostAP &&
            !(baseMaterial is null) &&
            !baseMaterial.IsEmpty &&
            baseMaterial.Model.ItemBase.Value is Equipment equipment &&
            equipment.level < 10 &&
            otherMaterials.Count(e => !e.IsEmpty) > 0 &&
            Widget.Find<Combination>().selectedIndex >= 0;

        protected override void Awake()
        {
            base.Awake();

            if (baseMaterial is null)
                throw new SerializeFieldNullException();

            baseMaterial.titleText.text =
                L10nManager.Localize("UI_ENHANCEMENT_EQUIPMENT_TO_ENHANCE");
            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.titleText.text =
                    L10nManager.Localize("UI_ENHANCEMENT_EQUIPMENT_TO_CONSUME");
            }

            message.SetActive(false);
            submitButton.SetSubmitText(L10nManager.Localize("UI_COMBINATION_ENHANCEMENT"));
        }

        public override bool Show(bool forced = false)
        {
            if (!base.Show(forced))
                return false;

            baseMaterial.Unlock(false);

            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.Lock();
            }

            return true;
        }

        public override bool DimFunc(InventoryItem inventoryItem)
        {
            if (!IsThereAnyUnlockedEmptyMaterialView)
                return true;

            var item = inventoryItem.ItemBase.Value;
            if (item.ItemType != ItemType.Equipment)
                return true;

            if (!baseMaterial.IsEmpty)
            {
                if (Contains(inventoryItem))
                    return true;

                var baseEquipment = (Equipment) baseMaterial.Model.ItemBase.Value;
                if (baseEquipment.ItemSubType != item.ItemSubType ||
                    baseEquipment.Grade != item.Grade)
                    return true;

                var material = (Equipment) inventoryItem.ItemBase.Value;
                if (baseEquipment.level != material.level)
                    return true;
            }

            return false;
        }

        protected override BigInteger GetCostNCG()
        {
            if (baseMaterial.IsEmpty ||
                !(baseMaterial.Model.ItemBase.Value is Equipment equipment) ||
                equipment.level >= 10)
                return 0;

            var row = Game.Game.instance.TableSheets
                .EnhancementCostSheet.Values
                .FirstOrDefault(x => x.Grade == equipment.Grade && x.Level == equipment.level + 1);

            return row is null ? 0 : row.Cost;
        }

        protected override int GetCostAP()
        {
            return baseMaterial.IsEmpty ? 0 : ItemEnhancement.GetRequiredAp();
        }

        protected override bool TryAddBaseMaterial(InventoryItem viewModel, int count,
            out EnhancementMaterialView materialView)
        {
            if (viewModel is null ||
                viewModel.ItemBase.Value.ItemType != ItemType.Equipment)
            {
                materialView = null;
                return false;
            }

            if (!baseMaterial.IsEmpty)
            {
                materialView = null;
                return false;
            }

            if (!base.TryAddBaseMaterial(viewModel, count, out materialView))
                return false;

            if (!(viewModel.ItemBase.Value is Equipment equipment))
                throw new InvalidCastException(nameof(viewModel.ItemBase.Value));

            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.Unlock(false);
            }

            UpdateMessageText();

            return true;
        }

        protected override bool TryRemoveBaseMaterial(EnhancementMaterialView view,
            out EnhancementMaterialView materialView)
        {
            if (!base.TryRemoveBaseMaterial(view, out materialView))
                return false;

            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.Clear();
                otherMaterial.Lock();
            }

            UpdateMessageText();

            return true;
        }

        protected override bool TryAddOtherMaterial(InventoryItem viewModel, int count,
            out EnhancementMaterialView materialView)
        {
            if (!base.TryAddOtherMaterial(viewModel, count, out materialView))
                return false;

            var equipment = (Equipment) baseMaterial.Model.ItemBase.Value;
            var statValue = equipment.StatsMap.GetStat(equipment.UniqueStatType, true);
            var resultValue = statValue + (int) equipment.GetIncrementAmountOfEnhancement();
            baseMaterial.UpdateStatView(resultValue.ToString(CultureInfo.InvariantCulture));
            UpdateMessageText();

            return true;
        }

        protected override bool TryRemoveOtherMaterial(EnhancementMaterialView view,
            out EnhancementMaterialView materialView)
        {
            if (!base.TryRemoveOtherMaterial(view, out materialView))
                return false;

            baseMaterial.UpdateStatView();
            UpdateMessageText();

            return true;
        }

        private void UpdateMessageText()
        {
            if (baseMaterial.IsEmpty)
            {
                message.SetActive(false);
                return;
            }

            if (!(baseMaterial.Model.ItemBase.Value is Equipment baseEquipment))
                throw new InvalidCastException(nameof(baseMaterial.Model.ItemBase.Value));

            var count = baseEquipment.GetOptionCount();
            foreach (var otherMaterial in otherMaterials.Where(e => !e.IsLocked && !e.IsEmpty))
            {
                if (!(otherMaterial.Model.ItemBase.Value is Equipment otherEquipment))
                    throw new InvalidCastException(nameof(otherMaterial.Model.ItemBase.Value));

                count = Math.Max(count, otherEquipment.GetOptionCount());
            }

            if (count == 0)
                return;

            message.SetActive(true);
            messageText.text = string.Format(
                L10nManager.Localize("UI_ENHANCEMENT_GUIDE"),
                count);
        }
    }
}
