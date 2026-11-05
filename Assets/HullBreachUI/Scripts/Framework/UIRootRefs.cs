using UnityEngine;

namespace HullBreach.UI.Framework
{
    public sealed class UIRootRefs
    {
        public GameObject RootObject;
        public Canvas Canvas;
        public RectTransform CanvasRect;
        public RectTransform BackdropLayer;
        public RectTransform SafeArea;
        public RectTransform ScreensLayer;
        public RectTransform FxLayer;
        public RectTransform ModalLayer;
        public RectTransform OverlayLayer;
        public Camera WorldCamera;

        public Camera ResolveCamera()
        {
            return WorldCamera != null ? WorldCamera : Camera.main;
        }
    }
}
