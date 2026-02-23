using System.Collections;
using System.Collections.Generic;
using _Main.Scripts.Datas;
using Fiber.UI;
using Fiber.Utilities;
using UnityEngine;

namespace _Main.Scripts.Manager
{
    public class ReferenceManager : SingletonInit<ReferenceManager>
    {
        #region Serialized Fields
        [SerializeField] private LoadingPanelController loadingPanelController;
        #endregion

        #region Properties
        public LoadingPanelController LoadingPanelController => loadingPanelController;
        #endregion
    }
}