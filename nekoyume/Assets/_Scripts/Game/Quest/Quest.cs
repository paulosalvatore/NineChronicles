using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public abstract class Quest
    {
        protected Quest(Data.Table.Quest data)
        {
            goal = data.goal;
            reward = data.reward;
        }

        public int goal;
        public decimal reward;
        public bool Complete { get; protected set; }

        public abstract void Check(Player player, List<List<ItemBase>> rewards);
        public abstract string ToInfo();
    }

    [Serializable]
    public class QuestList : IEnumerable<Quest>
    {
        public QuestList()
        {
            quests = new List<Quest>();
            foreach (var data in Tables.instance.Quest.Values)
            {
                var quest = new BattleQuest(data);
                quests.Add(quest);
            }

            foreach (var collectData in Tables.instance.CollectQuest.Values)
            {
                var quest = new CollectQuest(collectData);
                quests.Add(quest);
            }
        }

        private readonly List<Quest> quests;

        public void Update(Player player, List<List<ItemBase>> rewards)
        {
            foreach (var quest in quests)
            {
                quest.Check(player, rewards);
            }
        }

        public IEnumerator<Quest> GetEnumerator()
        {
            return quests.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
