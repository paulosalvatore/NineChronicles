using DG.Tweening;
using Nekoyume.Game;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class CriticalText : HudWidget
    {
        private const float TweenDuration = 0.3f;
        private const float DestroyDelay = 0.8f;
        
        private static readonly Vector3 LocalScaleBefore = new Vector3(2.4f, 2.4f, 1f);
        private static readonly Vector3 LocalScaleAfter = new Vector3(1.4f, 1.4f, 1f);
        
        public TextMeshProUGUI label;
        public TextMeshProUGUI shadow;
        public CanvasGroup group;

        public static CriticalText Show(Vector3 position, Vector3 force, string text)
        {
            var result = Create<CriticalText>(true);
            result.label.text = text;
            result.shadow.text = text;
            
            var rect = result.RectTransform;
            rect.anchoredPosition = position.ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
            rect.localScale = LocalScaleBefore;

            var tweenPos = (position + force).ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
            rect.DOAnchorPos(tweenPos, TweenDuration * 2.0f).SetEase(Ease.InOutQuad);
            rect.DOScale(LocalScaleAfter, TweenDuration).SetEase(Ease.OutCubic);
            result.group.DOFade(0.0f, TweenDuration).SetDelay(TweenDuration).SetEase(Ease.InCirc);
            
            Destroy(result.gameObject, DestroyDelay);

            return result;
        }
    }
}
