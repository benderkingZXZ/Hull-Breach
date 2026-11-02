using System;
using HullBreach.UI.Contracts;
using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HullBreach.UI.Controls
{
    /// <summary>
    /// Виртуальный стик слева. Зона касания — левые 46% безопасной области и нижние 62%: палец можно ставить
    /// в любое место зоны. Фиксированный режим: стик на месте, вектор считается от его центра.
    /// Плавающий режим: стик появляется под пальцем. Вектор отправляется в ICombatInputSink КАЖДЫЙ кадр,
    /// пока палец на экране (бэкенд обнуляет ввод при смене комнат, поэтому разовой отправки недостаточно).
    /// Работает одновременно с кнопками: каждый палец имеет свой pointerId.
    /// </summary>
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float BaseSize = 270f;
        private static readonly Vector2 IdlePosition = new Vector2(250f, 250f);
        private const float DeadZone = 0.12f;

        private RectTransform zone;
        private RectTransform baseRect;
        private RectTransform knobRect;
        private CanvasGroup baseGroup;
        private ICombatInputSink sink;

        private int pointerId = int.MinValue;
        private bool active;
        private bool floating;
        private float radius = BaseSize * 0.5f;
        private float targetAlpha = 0.5f;
        private float opacity = 0.85f;
        private Vector2 value;

        public Vector2 Value { get { return value; } }

        public static VirtualJoystick Create(RectTransform parent, ICombatInputSink sink)
        {
            RectTransform zone = UIFactory.Rect("JoystickZone", parent);
            zone.pivot = Vector2.zero;                                   // локальные координаты — от левого нижнего угла
            UIFactory.AnchorRange(zone, 0f, 0f, 0.46f, 0.62f, 0f, 0f, 0f, 0f);
            UIFactory.AddImage(zone.gameObject, ProceduralSprites.White, new Color(1f, 1f, 1f, 0f), Image.Type.Simple, true);

            RectTransform baseRt = UIFactory.Rect("Base", zone);
            UIFactory.Anchor(baseRt, 0f, 0f, 0.5f, 0.5f, IdlePosition.x, IdlePosition.y, BaseSize, BaseSize);
            UIFactory.AddImage(baseRt.gameObject, ProceduralSprites.Circle, UIColors.WithAlpha(UIColors.Hull, 0.55f));
            UIFactory.Img(baseRt, "Ring", ProceduralSprites.Ring, UIColors.WithAlpha(UIColors.Cyan, 0.75f));
            Image inner = UIFactory.Img(baseRt, "InnerRing", ProceduralSprites.Ring, UIColors.WithAlpha(UIColors.Cyan, 0.2f));
            UIFactory.Stretch(inner.rectTransform, 62f, 62f, 62f, 62f);
            CanvasGroup group = baseRt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            RectTransform knob = UIFactory.Rect("Knob", baseRt);
            UIFactory.Anchor(knob, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 118f, 118f);
            UIFactory.AddImage(knob.gameObject, ProceduralSprites.Circle, UIColors.WithAlpha(UIColors.Cyan, 0.8f));
            Image knobRing = UIFactory.Img(knob, "KnobRing", ProceduralSprites.Ring, UIColors.WithAlpha(Color.white, 0.8f));
            knobRing.raycastTarget = false;

            VirtualJoystick joystick = zone.gameObject.AddComponent<VirtualJoystick>();
            joystick.zone = zone;
            joystick.baseRect = baseRt;
            joystick.knobRect = knob;
            joystick.baseGroup = group;
            joystick.sink = sink;
            joystick.ApplySettings();
            return joystick;
        }

        private void OnEnable()
        {
            UISettings.Changed += ApplySettings;
            ApplySettings();
        }

        private void OnDisable()
        {
            UISettings.Changed -= ApplySettings;
            Release();
        }

        private void ApplySettings()
        {
            if (baseRect == null) return;
            floating = UISettings.FloatingJoystick;
            opacity = UISettings.ControlOpacity;
            float scale = UISettings.ControlScale;
            baseRect.localScale = new Vector3(scale, scale, 1f);
            radius = BaseSize * 0.5f * scale;
            if (!active) baseRect.anchoredPosition = IdlePosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (active) return;
            Vector2 local;
            if (!ToLocal(eventData, out local)) return;

            active = true;
            pointerId = eventData.pointerId;

            if (floating)
            {
                Vector2 size = zone.rect.size;
                float margin = radius * 0.8f;
                baseRect.anchoredPosition = new Vector2(
                    Mathf.Clamp(local.x, margin, Mathf.Max(margin, size.x - margin)),
                    Mathf.Clamp(local.y, margin, Mathf.Max(margin, size.y - margin)));
            }
            else
            {
                baseRect.anchoredPosition = IdlePosition;
            }
            UpdateFrom(local);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!active || eventData.pointerId != pointerId) return;
            Vector2 local;
            if (ToLocal(eventData, out local)) UpdateFrom(local);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!active || eventData.pointerId != pointerId) return;
            Release();
        }

        private bool ToLocal(PointerEventData e, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out local);
        }

        private void UpdateFrom(Vector2 local)
        {
            Vector2 offset = local - baseRect.anchoredPosition;
            Vector2 n = offset / radius;
            if (n.sqrMagnitude > 1f) n.Normalize();
            knobRect.anchoredPosition = n * (BaseSize * 0.5f);

            float mag = n.magnitude;
            if (mag < DeadZone) value = Vector2.zero;
            else value = n / mag * ((mag - DeadZone) / (1f - DeadZone));
        }

        private void Release()
        {
            bool wasActive = active;
            active = false;
            pointerId = int.MinValue;
            value = Vector2.zero;
            if (knobRect != null) knobRect.anchoredPosition = Vector2.zero;
            if (baseRect != null) baseRect.anchoredPosition = IdlePosition;
            if (wasActive && sink != null) sink.SetMoveInput(Vector2.zero);
        }

        private void Update()
        {
            if (active && sink != null) sink.SetMoveInput(value);

            if (baseGroup == null) return;
            targetAlpha = active ? opacity : (floating ? opacity * 0.35f : opacity * 0.6f);
            baseGroup.alpha = Mathf.MoveTowards(baseGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 5f);
        }
    }
}
