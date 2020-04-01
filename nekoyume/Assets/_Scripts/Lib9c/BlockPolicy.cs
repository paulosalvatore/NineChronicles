using System;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.TableData;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Nekoyume.BlockChain
{
    public class BlockPolicy
    {
        private static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(10);

        private static readonly ActionRenderer ActionRenderer = new ActionRenderer(
            ActionBase.RenderSubject,
            ActionBase.UnrenderSubject
        );

        public static WhiteListSheet WhiteListSheet { get; set; }

        public static WhiteListSheet GetWhiteListSheet(IValue state)
        {
            if (state is null)
            {
                return null;
            }

            var tableSheetsState = new TableSheetsState((Dictionary)state);
            return TableSheets.FromTableSheetsState(tableSheetsState).WhiteListSheet;
        }

        // FIXME 남은 설정들도 설정화 해야 할지도?
        public static IBlockPolicy<PolymorphicAction<ActionBase>> GetPolicy(int miniumDifficulty)
        {
#if UNITY_EDITOR
            return new DebugPolicy();
#else
            ActionRenderer
                .EveryRender(TableSheetsState.Address)
                .Subscribe(UpdateWhiteListSheet);

            ActionRenderer
                .EveryUnrender(TableSheetsState.Address)
                .Subscribe(UpdateWhiteListSheet);

            return new BlockPolicy<PolymorphicAction<ActionBase>>(
                new RewardGold { Gold = 1 },
                BlockInterval,
                miniumDifficulty,
                2048,
                doesTransactionFollowPolicy: IsSignerAuthorized
            );
#endif
        }

        private static bool IsSignerAuthorized(Transaction<PolymorphicAction<ActionBase>> transaction)
        {
            var signerPublicKey = transaction.PublicKey;

            return WhiteListSheet is null
                   || WhiteListSheet.Count == 0
                   || WhiteListSheet.Values.Any(row => signerPublicKey.Equals(row.PublicKey));
        }

        private static void UpdateWhiteListSheet(ActionBase.ActionEvaluation<ActionBase> evaluation)
        {
            var state = evaluation.OutputStates.GetState(TableSheetsState.Address);
            WhiteListSheet = GetWhiteListSheet(state);
        }

        private class DebugPolicy : IBlockPolicy<PolymorphicAction<ActionBase>>
        {
            public IAction BlockAction { get; } = new RewardGold {Gold = 1};

            public InvalidBlockException ValidateNextBlock(
                BlockChain<PolymorphicAction<ActionBase>> blocks,
                Block<PolymorphicAction<ActionBase>> nextBlock
            )
            {
                return null;
            }

            public long GetNextBlockDifficulty(BlockChain<PolymorphicAction<ActionBase>> blocks)
            {
                return blocks.Tip is null ? 0 : 1;
            }

            public bool DoesTransactionFollowsPolicy(
                Transaction<PolymorphicAction<ActionBase>> transaction
            ) =>
                true;
        }
    }
}
