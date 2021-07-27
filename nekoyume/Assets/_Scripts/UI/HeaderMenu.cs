using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.State;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class HeaderMenu : Widget
    {
        public enum ToggleType
        {
            Quest,
            AvatarInfo,
            CombinationSlots,
            Mail,
            Rank,
            Chat,
            Settings,
            Quit,
        }

        public enum AssetVisibleState
        {
            Main,
            Combination,
            Shop,
            Battle,
        }

        [Serializable]
        private class ToggleInfo
        {
            public ToggleType Type;
            public Toggle Toggle;
            public Image Notification;
        }

        public override WidgetType WidgetType => WidgetType.Popup;

        // 코드 보상 버튼
        public CodeRewardButton codeRewardButton;

        [SerializeField] private List<ToggleInfo> toggles = new List<ToggleInfo>();
        [SerializeField] private GameObject ncg;
        [SerializeField] private GameObject actionPoint;
        [SerializeField] private GameObject dailyBonus;
        [SerializeField] private GameObject hourglass;
        [SerializeField] private VFX inventoryVFX;
        [SerializeField] private VFX workshopVFX;
        [SerializeField] private Image actionPointImage;

        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();

        private readonly Dictionary<ToggleType, Widget> _toggleWidgets =
            new Dictionary<ToggleType, Widget>();

        private readonly Dictionary<ToggleType, ReactiveProperty<bool>> _toggleNotifications =
            new Dictionary<ToggleType, ReactiveProperty<bool>>()
            {
                {ToggleType.Quest, new ReactiveProperty<bool>(false)},
                {ToggleType.AvatarInfo, new ReactiveProperty<bool>(false)},
                {ToggleType.CombinationSlots, new ReactiveProperty<bool>(false)},
                {ToggleType.Mail, new ReactiveProperty<bool>(false)},
                {ToggleType.Rank, new ReactiveProperty<bool>(false)},
            };

        private readonly Dictionary<ToggleType, int> _toggleUnlockStages =
            new Dictionary<ToggleType, int>()
            {
                {ToggleType.Quest, GameConfig.RequireClearedStageLevel.UIBottomMenuQuest},
                {ToggleType.AvatarInfo, GameConfig.RequireClearedStageLevel.UIBottomMenuCharacter},
                {
                    ToggleType.CombinationSlots,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction
                },
                {ToggleType.Mail, GameConfig.RequireClearedStageLevel.UIBottomMenuMail},
                {ToggleType.Rank, 1},
                {ToggleType.Chat, GameConfig.RequireClearedStageLevel.UIBottomMenuChat},
                {ToggleType.Settings, 1},
                {ToggleType.Quit, 1},
            };

        private long _blockIndex;

        public Image ActionPointImage => actionPointImage;

        public override void Initialize()
        {
            base.Initialize();

            _toggleWidgets.Add(ToggleType.Quest, Find<Quest>());
            _toggleWidgets.Add(ToggleType.AvatarInfo, Find<AvatarInfo>());
            _toggleWidgets.Add(ToggleType.CombinationSlots, Find<CombinationSlots>());
            _toggleWidgets.Add(ToggleType.Mail, Find<Mail>());
            _toggleWidgets.Add(ToggleType.Rank, Find<Rank>());
            _toggleWidgets.Add(ToggleType.Settings, Find<Settings>());
            _toggleWidgets.Add(ToggleType.Chat, Find<ChatPopup>());
            _toggleWidgets.Add(ToggleType.Quit, Find<QuitPopup>());

            foreach (var toggleInfo in toggles)
            {
                if (_toggleNotifications.ContainsKey(toggleInfo.Type))
                {
                    _toggleNotifications[toggleInfo.Type].SubscribeTo(toggleInfo.Notification)
                        .AddTo(gameObject);
                }

                toggleInfo.Toggle.onValueChanged.AddListener((value) =>
                {
                    var widget = _toggleWidgets[toggleInfo.Type];
                    if (value)
                    {
                        var requiredStage = _toggleUnlockStages[toggleInfo.Type];
                        if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(
                            requiredStage))
                        {
                            var msg = string.Format(L10nManager.Localize("UI_STAGE_LOCK_FORMAT"),
                                requiredStage);
                            OneLinePopup.Push(MailType.System, msg);
                            toggleInfo.Toggle.isOn = false;
                            return;
                        }

                        widget.Show(() => { toggleInfo.Toggle.isOn = false; });
                    }
                    else
                    {
                        if (widget.isActiveAndEnabled)
                        {
                            widget.Close(true);
                        }
                    }
                });
            }

            Game.Event.OnRoomEnter.AddListener(_ => UpdateAssets(AssetVisibleState.Main));
            Game.Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(SubscribeBlockIndex)
                .AddTo(gameObject);

            CloseWidget = null;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _disposablesAtOnEnable.DisposeAllAndClear();
            ReactiveAvatarState.QuestList?.Subscribe(SubscribeAvatarQuestList)
                .AddTo(_disposablesAtOnEnable);
            ReactiveAvatarState.MailBox?.Subscribe(SubscribeAvatarMailBox)
                .AddTo(_disposablesAtOnEnable);
            ReactiveAvatarState.Inventory?.Subscribe(SubscribeInventory)
                .AddTo(_disposablesAtOnEnable);
        }

        protected override void OnDisable()
        {
            _disposablesAtOnEnable.DisposeAllAndClear();
            base.OnDisable();
        }

        public void PlayVFX(ItemMoveAnimation.EndPoint endPoint)
        {
            switch (endPoint)
            {
                case ItemMoveAnimation.EndPoint.Inventory:
                    inventoryVFX.Play();
                    break;
                case ItemMoveAnimation.EndPoint.Workshop:
                    workshopVFX.Play();
                    break;
            }
        }

        public Transform GetToggle(ToggleType toggleType)
        {
            var info = toggles.FirstOrDefault(x => x.Type.Equals(toggleType));
            var toggleTransform = info?.Toggle.transform;
            return toggleTransform ? toggleTransform : null;
        }

        public void UpdateAssets(AssetVisibleState state)
        {
            switch (state)
            {
                case AssetVisibleState.Main:
                    SetActiveAssets(true, true, true, false);
                    break;
                case AssetVisibleState.Combination:
                    SetActiveAssets(true, true, false, true);
                    break;
                case AssetVisibleState.Shop:
                case AssetVisibleState.Battle:
                    SetActiveAssets(true, true, false, false);
                    break;
            }
        }

        private void SetActiveAssets(bool isNcgActive, bool isActionPointActive,
            bool isDailyBonusActive, bool isHourglassActive)
        {
            ncg.SetActive(isNcgActive);
            actionPoint.SetActive(isActionPointActive);
            dailyBonus.SetActive(isDailyBonusActive);
            hourglass.SetActive(isHourglassActive);
        }

        private void SubscribeBlockIndex(long blockIndex)
        {
            _blockIndex = blockIndex;
            UpdateCombinationNotification();

            var mailBox = Find<Mail>().MailBox;
            if (mailBox is null)
            {
                return;
            }

            _toggleNotifications[ToggleType.Mail].Value =
                mailBox.Any(i => i.New && i.requiredBlockIndex <= _blockIndex);
            ;
        }

        private void SubscribeAvatarMailBox(MailBox mailBox)
        {
            if (mailBox is null)
            {
                Debug.LogWarning($"{nameof(mailBox)} is null.");
                return;
            }

            _toggleNotifications[ToggleType.Mail].Value =
                mailBox.Any(i => i.New && i.requiredBlockIndex <= _blockIndex);
        }

        private void SubscribeAvatarQuestList(QuestList questList)
        {
            if (questList is null)
            {
                Debug.LogWarning($"{nameof(questList)} is null.");
                return;
            }

            var hasNotification =
                questList.Any(quest => quest.IsPaidInAction && quest.isReceivable);
            _toggleNotifications[ToggleType.Quest].Value = hasNotification;
            Find<Quest>().SetList(questList);
        }

        private void SubscribeInventory(Nekoyume.Model.Item.Inventory inventory)
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var avatarLevel = States.Instance.CurrentAvatarState?.level ?? 0;
            var hasNotification = inventory?.HasNotification(avatarLevel, blockIndex) ?? false;
            UpdateInventoryNotification(hasNotification);
        }

        public void UpdateCombinationNotification()
        {
            var combinationSlots = Find<CombinationSlots>().slots;
            var hasNotification = combinationSlots.Any(slot => slot.HasNotification.Value);
            _toggleNotifications[ToggleType.CombinationSlots].Value = hasNotification;
        }

        public void UpdateInventoryNotification(bool hasNotification)
        {
            _toggleNotifications[ToggleType.AvatarInfo].Value = hasNotification;
        }

        public void TutorialActionClickBottomMenuWorkShopButton()
        {
            var info = toggles.FirstOrDefault(x => x.Type.Equals(ToggleType.CombinationSlots));
            if (info != null)
            {
                info.Toggle.isOn = true;
            }
        }

        public void TutorialActionClickBottomMenuMailButton()
        {
            var info = toggles.FirstOrDefault(x => x.Type.Equals(ToggleType.Mail));
            if (info != null)
            {
                info.Toggle.isOn = true;
            }
        }

        public void TutorialActionClickBottomMenuCharacterButton()
        {
            var info = toggles.FirstOrDefault(x => x.Type.Equals(ToggleType.AvatarInfo));
            if (info != null)
            {
                info.Toggle.isOn = true;
            }
        }
    }
}