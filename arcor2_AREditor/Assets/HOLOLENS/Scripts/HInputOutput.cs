using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Base;
using static Base.Clickable;
public abstract class HInputOutput : MonoBehaviour
{
        public HAction Action;
        private List<string> logicItemIds = new List<string>();
        [SerializeField]
      //  private OutlineOnClick outlineOnClick;

        public object ifValue;


        public void AddLogicItem(string logicItemId) {
            Debug.Assert(logicItemId != null);
            logicItemIds.Add(logicItemId);
        }

        public void RemoveLogicItem(string logicItemId) {
            Debug.Assert(logicItemIds.Contains(logicItemId));
            logicItemIds.Remove(logicItemId);
        }

        public List<HLogicItem> GetLogicItems() {
            List<HLogicItem> items = new List<HLogicItem>();
            foreach (string itemId in logicItemIds)
                if (HProjectManager.Instance.LogicItems.TryGetValue(itemId, out HLogicItem logicItem)) {
                    items.Add(logicItem);
                } else {
                    throw new ItemNotFoundException("Logic item with ID " + itemId + " does not exists");
                }
            return items;
        }

        protected bool CheckClickType(Click type) {
           
          /*  if (!(bool) MainSettingsMenu.Instance.ConnectionsSwitch.GetValue()) {
                return false;
            }*/
            if (GameManagerH.Instance.GetGameState() != GameManagerH.GameStateEnum.ProjectEditor) {
                return false;
            }
            if (type != Click.MOUSE_LEFT_BUTTON && type != Click.TOUCH) {
                return false;
            }
            return true;
        }

        public HInteractiveObject GetParentObject() {
            return Action;
        }

        public bool AnyConnection() {
            return ConnectionCount() > 0;
        }

        public int ConnectionCount() {
            return logicItemIds.Count();
        }
}
