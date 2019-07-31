﻿using Nekoyume.Data;
using Nekoyume.Game.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class Recipe : MonoBehaviour
    {
        public RecipeScrollerController scrollerController;

        #region Mono

        private void Awake()
        {
            var recipeInfoList = new List<RecipeInfo>();
            var recipeTable = Tables.instance.Recipe;
            foreach (var pair in recipeTable)
            {
                var info = new RecipeInfo
                {
                    id = pair.Value.Id,
                    resultName = GetItemName(pair.Value.ResultId),
                    resultSprite = ItemBase.GetSprite(pair.Value.ResultId),
                };
                info.materialSprites[0] = ItemBase.GetSprite(pair.Value.Material1);
                info.materialSprites[1] = ItemBase.GetSprite(pair.Value.Material2);
                info.materialSprites[2] = ItemBase.GetSprite(pair.Value.Material3);
                info.materialSprites[3] = ItemBase.GetSprite(pair.Value.Material4);
                info.materialSprites[4] = ItemBase.GetSprite(pair.Value.Material5);

                recipeInfoList.Add(info);
            }
            recipeInfoList.Sort((x, y) => x.id - y.id);
            scrollerController.SetData(recipeInfoList);
        }

        #endregion

        private string GetItemName(int id)
        {
            if (id == 0) return string.Empty;

            string name = string.Empty;
            if (TryGetEquipmentName(id, out name))
            {
                return name;
            }
            else if (TryGetMaterialName(id, out name))
            {
                return name;
            }
            else
            {
                Debug.LogError("Item not found!");
                return string.Empty;
            }
        }

        private bool TryGetEquipmentName(int id, out string name)
        {
            var equips = Tables.instance.ItemEquipment;

            if (equips.ContainsKey(id))
            {
                name = equips[id].name;
                return true;
            }
            name = string.Empty;
            return false;
        }

        private bool TryGetMaterialName(int id, out string name)
        {
            var items = Tables.instance.Item;

            if (items.ContainsKey(id))
            {
                name = items[id].name;
                return true;
            }
            name = string.Empty;
            return false;
        }
    }
}
