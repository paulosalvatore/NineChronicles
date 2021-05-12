using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Shop : IDisposable
    {
        public const int MinimumPrice = 10;

        public readonly ReactiveProperty<ItemCountAndPricePopup> ItemCountAndPricePopup =
            new ReactiveProperty<ItemCountAndPricePopup>(new ItemCountAndPricePopup());

        public void Dispose()
        {
            ItemCountAndPricePopup.DisposeAll();
        }
    }
}
