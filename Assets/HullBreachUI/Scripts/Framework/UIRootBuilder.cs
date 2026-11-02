using HullBreach.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Framework
{
    /// <summary>
    /// Создаёт корневой Canvas: Screen Space Overlay, CanvasScaler 1920×1080 (match по высоте),
    /// слои и SafeArea. Никаких ручных настроек в сцене не требуется.
    /// </summary>
    public static class UIRootBuilder
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        public static UIRootRefs Build(int sortingOrder, Camera worldCamera)
        {
            GameObject go = new GameObject("HullBreachUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;   // высота всегда 1080 единиц, ширина подстраивается под экран (4:3 … 21:9)

            UIRootRefs r = new UIRootRefs();
            r.RootObject = go;
            r.Canvas = canvas;
            r.CanvasRect = (RectTransform)go.transform;
            r.WorldCamera = worldCamera;

            StarfieldBackdrop backdrop = StarfieldBackdrop.Build(r.CanvasRect);
            r.BackdropLayer = (RectTransform)backdrop.transform;

            r.SafeArea = UIFactory.Rect("SafeArea", r.CanvasRect);
            r.SafeArea.gameObject.AddComponent<SafeAreaFitter>();
            r.ScreensLayer = UIFactory.Rect("Screens", r.SafeArea);
            UIFactory.Stretch(r.ScreensLayer);

            r.FxLayer = UIFactory.Rect("Fx", r.CanvasRect);
            UIFactory.Stretch(r.FxLayer);
            r.ModalLayer = UIFactory.Rect("Modals", r.CanvasRect);
            UIFactory.Stretch(r.ModalLayer);
            r.OverlayLayer = UIFactory.Rect("Overlay", r.CanvasRect);
            UIFactory.Stretch(r.OverlayLayer);
            return r;
        }
    }
}
