using UnityEngine;

namespace HullBreach.UI.Framework
{
    /// <summary>Слои корневого Canvas (порядок отрисовки: Backdrop → SafeArea → Fx → Modals → Overlay).</summary>
    public sealed class UIRootRefs
    {
        public GameObject RootObject;
        public Canvas Canvas;
        public RectTransform CanvasRect;
        /// <summary>Фон меню на весь экран (отключается в бою).</summary>
        public RectTransform BackdropLayer;
        /// <summary>Область, свободная от вырезов и системных жестов. Внутри — все экраны.</summary>
        public RectTransform SafeArea;
        public RectTransform ScreensLayer;
        /// <summary>Полноэкранные эффекты HUD: виньетка урона, всплывающие числа, маркер цели.</summary>
        public RectTransform FxLayer;
        public RectTransform ModalLayer;
        public RectTransform OverlayLayer;
        /// <summary>Камера мира для перевода координат врагов в экранные. Если null — Camera.main.</summary>
        public Camera WorldCamera;

        public Camera ResolveCamera()
        {
            return WorldCamera != null ? WorldCamera : Camera.main;
        }
    }
}
