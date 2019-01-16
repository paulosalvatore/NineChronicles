using System.Collections;
using System.Threading;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Trigger;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class Move : Widget
    {
        public GameObject btnMove;
        public GameObject btnStage1;
        public GameObject btnSleep;

        public Text LabelInfo;

        public void ShowRoom()
        {
            Show();
            btnSleep.GetComponent<Button>().enabled = true;
            btnMove.GetComponent<Button>().enabled = true;
            Model.Avatar avatar = ActionManager.Instance.Avatar;
            bool enabled = !avatar.Dead;
            btnMove.SetActive(enabled);
            btnStage1.SetActive(ActionManager.Instance.Avatar.WorldStage > 1);
            btnSleep.SetActive(false);
            LabelInfo.text = "";
        }

        public void ShowWorld()
        {
            Show();
            btnMove.SetActive(false);
            btnSleep.SetActive(true);
            LabelInfo.text = "";
        }

        public void MoveClick()
        {
            ActionManager.Instance.HackAndSlash();
            btnMove.SetActive(false);
        }

        public void SleepClick()
        {
            LabelInfo.text = "Go Home Soon...";
            this.GetRootComponent<Game.Game>().GetComponentInChildren<StageExit>().Sleep = true;
            btnSleep.SetActive(false);
        }

        public void MoveStageClick()
        {
            StartCoroutine(MoveStageAsync());
        }

        private IEnumerator MoveStageAsync()
        {
            ActionManager.Instance.MoveStage(1);
            while (ActionManager.Instance.Avatar.WorldStage != 1)
            {
                yield return new WaitForSeconds(1.0f);
            }
            Game.Event.OnStageEnter.Invoke();
        }
    }
}
