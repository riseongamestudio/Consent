using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RiseOn.Consent.Platforms.Editor {
    /// <summary>The look of the dialog a device shows in the stand-in's place.</summary>
    internal enum DialogLook {
        /// <summary>An iOS alert: a narrow card in the middle, its title and text centred, its buttons under thin lines, side by side when they fit.</summary>
        IOSAlert
      , /// <summary>An Android alert: a card in the middle, its title and text to the left, its first button at the left as the neutral one, the others at the right.</summary>
        AndroidAlert
    }

    /// <summary>One button of a dialog: what it does, whether the dialog stays up after, as a notice's policy button keeps it, and whether iOS sets it bold as the preferred one.</summary>
    internal readonly struct DialogChoice {
        public readonly string Text;
        public readonly Action Picked;
        public readonly bool   Stays;
        public readonly bool   Preferred;

        public DialogChoice(string text, Action picked, bool stays = false, bool preferred = false) {
            Text      = text;
            Picked    = picked;
            Stays     = stays;
            Preferred = preferred;
        }
    }

    /// <summary>
    /// The stand-in's dialogs as Play mode draws them: over everything in the Game view and taking every touch, as the platform's own take
    /// them on a device, sized in device points as on a phone 360 points across, in the look of the dialog they stand in for.
    /// </summary>
    internal static class ConsentEditorDialog {
        // Over every canvas a game or the ads pack draws, as a platform's dialog is over the whole app.
        private const int SortingOrder = short.MaxValue;

        private static readonly Color dim   = new(0, 0, 0, .4f);
        private static readonly Color clear = new(1, 1, 1, 0);

        // The platforms' light and dark appearance, as the devices draw these dialogs in each.
        private static readonly Palette light = new() {
            Card    = Color.white, Ink = new(.13f, .13f, .13f), Grey = new(.37f, .39f, .41f), Line = new(.85f, .86f, .88f)
          , Blue    = new(.1f, .45f, .91f), OnBlue = Color.white, AndroidCard = Color.white
          , Form    = new(.24f, .4f, .77f), OnForm = Color.white, Tint = new(.82f, .89f, .99f), Track = new(.74f, .76f, .78f)
          , IOSCard = new(.95f, .95f, .95f, .98f), IOSInk = Color.black, IOSLine = new(.24f, .24f, .26f, .29f), IOSBlue = new(0, .48f, 1)
        };

        private static readonly Palette dark = new() {
            Card    = new(.13f, .13f, .14f), Ink = new(.91f, .92f, .93f), Grey = new(.74f, .76f, .78f), Line = new(.24f, .25f, .27f)
          , Blue    = new(.54f, .71f, .97f), OnBlue = new(.13f, .13f, .14f), AndroidCard = new(.19f, .19f, .2f)
          , Form    = new(.54f, .67f, .97f), OnForm = new(.13f, .13f, .14f), Tint = new(.14f, .25f, .4f), Track = new(.37f, .39f, .41f)
          , IOSCard = new(.17f, .17f, .18f, .98f), IOSInk = Color.white, IOSLine = new(.33f, .33f, .35f, .6f), IOSBlue = new(.04f, .52f, 1)
        };

        // The appearance of the dialog being built: the Editor's own theme, read as it shows.
        private static Palette colors = light;

        // Rounded boxes by their corner in pixels, drawn once: a built-in sprite scaled up blurs its edges.
        private static readonly Dictionary<int, Sprite> rounded = new();

        /// <summary>An alert of the platform's: its title and message over its buttons.</summary>
        public static void Show(DialogLook look, string title, string message, params DialogChoice[] choices) {
            var root = Root();
            if (look is DialogLook.IOSAlert) IOSAlert(root, title, message, choices);
            else AndroidAlert(root, title, message, choices);
        }

        /// <summary>
        /// UMP's GDPR form as Google draws it: the welcome and the question, the two purposes with their icons and the Learn more line,
        /// the paragraph on vendors under a line, then the round filled buttons.
        /// </summary>
        public static void Gdpr(Action consent, Action doNotConsent) {
            var root = Root();
            var card = FormCard(root);
            var app  = Application.productName;
            Label(card, $"{app} asks for your consent to use your personal data to:", 20, colors.Ink, FontStyle.Bold, TextAnchor.UpperCenter);
            Purpose(card, Glyph.Person, "Personalised advertising and content, advertising and content measurement, audience research and services development");
            Purpose(card, Glyph.Devices, "Store and/or access information on a device");
            Purpose(card, Glyph.More, "Learn more");
            Label(card, "Your personal data will be processed, and information from your device (cookies, unique identifiers and other device data) may be stored, accessed and shared with third-party vendors, or used by this app alone.", 13, colors.Grey, FontStyle.Normal, TextAnchor.UpperLeft);
            Line(card, 1, 0, colors.Line);
            Pill(card, root, new DialogChoice("Consent", consent));
            Pill(card, root, new DialogChoice("Do not consent", doNotConsent));
        }

        /// <summary>
        /// UMP's US states form as Google draws it: the choices' title and what they are about, the switch to opt out of the sale and
        /// sharing of the player's data, set as last chosen, and the Confirm button, which answers whether the switch is on.
        /// </summary>
        public static void UsStates(bool optedOut, Action<bool> confirmed) {
            var root = Root();
            var card = FormCard(root);
            Label(card, "Your privacy choices", 20, colors.Ink, FontStyle.Bold, TextAnchor.UpperCenter);
            Label(card, "We and our partners may share your personal information to show you personalised ads. The privacy laws of some US states let you opt out of this sale or sharing.", 14, colors.Grey, FontStyle.Normal, TextAnchor.UpperLeft);
            var on = Switch(card, "Do not sell or share my personal information", optedOut);
            Line(card, 1, 0, colors.Line);
            Pill(card, root, new DialogChoice("Confirm", () => confirmed(on())));
        }

        // A dialog's canvas over everything, its shade taking every touch, in the Editor's theme.
        private static GameObject Root() {
            colors = EditorGUIUtility.isProSkin ? dark : light;
            EnsureEventSystem();
            var root   = new GameObject("Consent Dialog", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            UnityEngine.Object.DontDestroyOnLoad(root);

            var shade = Box(root.transform, dim);
            Stretch(shade.rectTransform);
            shade.raycastTarget = true;
            return root;
        }

        // A form's card, as wide as the screen less its margins, the welcome to the app at its top.
        private static Transform FormCard(GameObject root) {
            var card = Card(root.transform, colors.Card, 0, 16, 12, 24, 24, 16);
            Label(card, $"Welcome to {Application.productName}", 15, colors.Grey, FontStyle.Bold, TextAnchor.UpperCenter);
            return card;
        }

        // A purpose's line: its icon on a disc at the left, its text beside it.
        private static void Purpose(Transform card, Glyph glyph, string text) {
            var row = Group<HorizontalLayoutGroup>(card, 0, 0, 16);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment         = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = false;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(ConsentGlyph), typeof(LayoutElement));
            icon.transform.SetParent(row, false);
            var graphic = icon.GetComponent<ConsentGlyph>();
            graphic.Kind          = glyph;
            graphic.Disc          = glyph is Glyph.More ? colors.Card : colors.Tint;
            graphic.Rim           = glyph is Glyph.More ? colors.Line : colors.Tint;
            graphic.color         = glyph is Glyph.More ? colors.Grey : colors.Form;
            graphic.raycastTarget = false;
            var size = icon.GetComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = size.minHeight = size.preferredHeight = 36 * Points();

            var label = Label(row, text, 14, colors.Ink, FontStyle.Normal, TextAnchor.MiddleLeft);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        // A round filled button across the card.
        private static void Pill(Transform card, GameObject root, DialogChoice choice) {
            var button = ButtonBox(card, colors.Form, 44, 22, root, choice);
            Label(button.transform, choice.Text, 15, colors.OnForm, FontStyle.Bold, TextAnchor.MiddleCenter, true);
        }

        // A line of text with a switch at its right, the whole line toggling it; returns what the switch is set to.
        private static Func<bool> Switch(Transform card, string text, bool on) {
            var row = Group<HorizontalLayoutGroup>(card, 0, 4, 12);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment         = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = false;
            var hit = row.gameObject.AddComponent<Image>();
            hit.color = clear;

            var label = Label(row, text, 15, colors.Ink, FontStyle.Normal, TextAnchor.MiddleLeft);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var track = Box(row, colors.Track);
            Round(track, 16);
            var size = track.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 52 * Points();
            size.minHeight = size.preferredHeight = 32 * Points();
            var knob = Box(track.transform, Color.white);
            Round(knob, 12);
            var rect = knob.rectTransform;
            rect.sizeDelta = Vector2.one * 24 * Points();

            void Draw() {
                track.color    = on ? colors.Form : colors.Track;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(on ? 1 : 0, .5f);
                rect.anchoredPosition = new Vector2(on ? -4 : 4, 0) * Points();
            }

            row.gameObject.AddComponent<Button>().onClick.AddListener(() => {
                on = !on;
                Draw();
            });
            Draw();
            return () => on;
        }

        private static void IOSAlert(GameObject root, string title, string message, DialogChoice[] choices) {
            var card = Card(root.transform, colors.IOSCard, 270, 0, 14, 0, 0, 0);
            var head = Group<VerticalLayoutGroup>(card, 16, 20, 4);
            Label(head, title, 17, colors.IOSInk, FontStyle.Bold, TextAnchor.UpperCenter);
            Label(head, message, 13, colors.IOSInk, FontStyle.Normal, TextAnchor.UpperCenter);

            // iOS sets two buttons side by side unless one of them would not fit in its half.
            var sideBySide = choices.Length == 2;
            foreach (var choice in choices) sideBySide &= Measure(choice.Text, 17, FontStyle.Bold) <= (135 - 16) * Points();

            Line(card, 1, 0, colors.IOSLine);
            var row = sideBySide ? Group<HorizontalLayoutGroup>(card, 0, 0, 0) : card;
            for (var i = 0; i < choices.Length; i++) {
                if (i > 0) {
                    if (sideBySide) Line(row, 0, 1, colors.IOSLine);
                    else Line(row, 1, 0, colors.IOSLine);
                }

                var button = ButtonBox(row, clear, 44, 0, root, choices[i]);
                if (sideBySide) button.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;
                Label(button.transform, choices[i].Text, 17, colors.IOSBlue, choices[i].Preferred ? FontStyle.Bold : FontStyle.Normal, TextAnchor.MiddleCenter, true);
            }
        }

        private static void AndroidAlert(GameObject root, string title, string message, DialogChoice[] choices) {
            var card = Card(root.transform, colors.AndroidCard, 312, 0, 8, 24, 24, 16);
            Label(card, title, 20, colors.Ink, FontStyle.Normal, TextAnchor.UpperLeft);
            Label(card, message, 16, colors.Grey, FontStyle.Normal, TextAnchor.UpperLeft);
            var row = Group<HorizontalLayoutGroup>(card, 0, 0, 8);
            for (var i = 0; i < choices.Length; i++) {
                // Between the neutral button and the others, the space that pushes them to the right.
                if (i == 1) {
                    var space = new GameObject("Space", typeof(RectTransform), typeof(LayoutElement));
                    space.transform.SetParent(row, false);
                    space.GetComponent<LayoutElement>().flexibleWidth = 1;
                }

                var button = ButtonBox(row, clear, 40, 0, root, choices[i]);
                button.gameObject.GetComponent<LayoutElement>().preferredWidth = Measure(choices[i].Text, 14, FontStyle.Bold) + 24 * Points();
                Label(button.transform, choices[i].Text, 14, colors.Blue, FontStyle.Bold, TextAnchor.MiddleCenter, true);
            }
        }

        // A card in the middle: as wide as the points given, or the screen less the margins; as tall as its stacked children.
        private static Transform Card(Transform parent, Color color, float width, float margins, float corner, float sides, float ends, float spacing) {
            var card = Box(parent, color);
            Round(card, corner);
            card.raycastTarget = true;
            var rect = card.rectTransform;
            var wide = width <= 0;
            rect.anchorMin        = new Vector2(wide ? 0 : .5f, .5f);
            rect.anchorMax        = new Vector2(wide ? 1 : .5f, .5f);
            rect.pivot            = new Vector2(.5f, .5f);
            rect.sizeDelta        = new Vector2((wide ? -2 * margins : width) * Points(), 0);
            rect.anchoredPosition = Vector2.zero;
            Layout(card.gameObject.AddComponent<VerticalLayoutGroup>(), sides, ends, spacing);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return card.transform;
        }

        private static Transform Group<T>(Transform parent, float sides, float ends, float spacing) where T : HorizontalOrVerticalLayoutGroup {
            var group = new GameObject(typeof(T).Name, typeof(RectTransform));
            group.transform.SetParent(parent, false);
            Layout(group.AddComponent<T>(), sides, ends, spacing);
            return group.transform;
        }

        private static void Layout(HorizontalOrVerticalLayoutGroup layout, float sides, float ends, float spacing) {
            var scale = Points();
            int Scaled(float points) => Mathf.RoundToInt(points * scale);
            layout.padding                = new RectOffset(Scaled(sides), Scaled(sides), Scaled(ends), Scaled(ends));
            layout.spacing                = spacing * scale;
            layout.childControlWidth      = layout.childControlHeight = true;
            // A stack's children take its width, a row's its height, so a row's hairlines run from top to bottom.
            layout.childForceExpandWidth  = layout is VerticalLayoutGroup;
            layout.childForceExpandHeight = layout is HorizontalLayoutGroup;
        }

        // A hairline: one pixel high in a stack, one pixel wide in a row.
        private static void Line(Transform parent, float height, float width, Color color) {
            var element = Box(parent, color).gameObject.AddComponent<LayoutElement>();
            if (height > 0) element.preferredHeight = height;
            if (width > 0) element.preferredWidth = width;
        }

        // A button that takes the dialog down unless its choice stays, then runs the choice.
        private static Image ButtonBox(Transform parent, Color color, float height, float corner, GameObject dialog, DialogChoice choice) {
            var box = Box(parent, color);
            if (corner > 0) Round(box, corner);
            box.raycastTarget = true;
            box.gameObject.AddComponent<LayoutElement>().preferredHeight = height * Points();
            box.gameObject.AddComponent<Button>().onClick.AddListener(() => {
                if (!choice.Stays) UnityEngine.Object.Destroy(dialog);
                choice.Picked?.Invoke();
            });
            return box;
        }

        private static void Round(Image image, float corner) {
            image.sprite = Rounded(Mathf.Max(1, Mathf.RoundToInt(corner * Points())));
            image.type   = Image.Type.Sliced;
        }

        // A white box with corners of the radius, nine-sliced at it, its edge smoothed over one pixel.
        private static Sprite Rounded(int radius) {
            if (rounded.TryGetValue(radius, out var sprite) && sprite != null) return sprite;

            var size   = radius * 2 + 2;
            var half   = size / 2f;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++) {
                for (var x = 0; x < size; x++) {
                    var dx    = Mathf.Max(Mathf.Abs(x + .5f - half) - (half - radius), 0);
                    var dy    = Mathf.Max(Mathf.Abs(y + .5f - half) - (half - radius), 0);
                    var alpha = Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte) Mathf.RoundToInt(alpha * 255));
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            sprite           = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, Vector4.one * radius);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            rounded[radius]  = sprite;
            return sprite;
        }

        private static Image Box(Transform parent, Color color) {
            var image = new GameObject("Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color         = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text Label(Transform parent, string text, float points, Color color, FontStyle style, TextAnchor alignment, bool fill = false) {
            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            label.font               = Font;
            label.fontSize           = Mathf.RoundToInt(points * Points());
            label.fontStyle          = style;
            label.alignment          = alignment;
            label.color              = color;
            label.text               = text;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.raycastTarget      = false;
            if (fill) Stretch(label.rectTransform);
            return label;
        }

        // The width the text takes on one line, in pixels.
        private static float Measure(string text, float points, FontStyle style) {
            var settings = new TextGenerationSettings {
                font = Font, fontSize = Mathf.RoundToInt(points * Points()), fontStyle = style, scaleFactor = 1
              , horizontalOverflow = HorizontalWrapMode.Overflow, verticalOverflow = VerticalWrapMode.Overflow, richText = false
              , generationExtents = Vector2.zero, pivot = Vector2.zero, textAnchor = TextAnchor.UpperLeft, color = Color.white
            };
            return new TextGenerator().GetPreferredWidth(text, settings);
        }

        private static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static void Stretch(RectTransform rect) {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // Pixels per device point, as on a phone 360 points across, as the ads pack's placeholders are sized.
        private static float Points() {
            return Mathf.Min(Screen.width, Screen.height) / 360f;
        }

        // Without an event system nothing hears the buttons, and the game may not have made one yet.
        private static void EnsureEventSystem() {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;

            var system = new GameObject(nameof(EventSystem), typeof(EventSystem));
            UnityEngine.Object.DontDestroyOnLoad(system);
#if ENABLE_INPUT_SYSTEM && CONSENT_SUPPORT_INPUTSYSTEM
            system.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            system.AddComponent<StandaloneInputModule>();
#endif
        }

        private sealed class Palette {
            public Color Card, Ink, Grey, Line, Blue, OnBlue, AndroidCard, Form, OnForm, Tint, Track, IOSCard, IOSInk, IOSLine, IOSBlue;
        }
    }

    /// <summary>The icons of UMP's purposes: a person, a laptop with a phone, and the chevron of Learn more.</summary>
    internal enum Glyph {
        Person
      , Devices
      , More
    }

    /// <summary>A purpose's icon as UMP draws it: on a disc, a ring around the chevron's, in strokes and shapes rather than a font's glyphs.</summary>
    internal sealed class ConsentGlyph : MaskableGraphic {
        public Glyph Kind;
        public Color Disc;
        public Color Rim;

        protected override void OnPopulateMesh(VertexHelper mesh) {
            mesh.Clear();
            var rect   = GetPixelAdjustedRect();
            var radius = Mathf.Min(rect.width, rect.height) / 2;
            var center = rect.center;
            Vector2 At(float x, float y) => center + new Vector2(x, y) * radius;
            var stroke = radius * .07f;

            Circle(mesh, center, radius, Rim);
            Circle(mesh, center, radius - Mathf.Max(1, radius * .04f), Disc);
            switch (Kind) {
                case Glyph.Person:
                    Ring(mesh, At(0, .2f), radius * .2f, stroke, color);
                    for (var i = 0; i < 12; i++) {
                        var from = Mathf.PI * i / 12;
                        var to   = Mathf.PI * (i + 1) / 12;
                        Bar(mesh, At(Mathf.Cos(from) * .38f, -.42f + Mathf.Sin(from) * .3f), At(Mathf.Cos(to) * .38f, -.42f + Mathf.Sin(to) * .3f), stroke, color);
                    }

                    Bar(mesh, At(-.38f, -.42f), At(.38f, -.42f), stroke, color);
                    break;
                case Glyph.Devices:
                    Box(mesh, At(-.46f, -.18f), At(.14f, .3f), stroke, color);
                    Bar(mesh, At(-.56f, -.28f), At(.2f, -.28f), stroke, color);
                    Box(mesh, At(.24f, -.3f), At(.5f, .14f), stroke, color);
                    break;
                default:
                    Bar(mesh, At(-.22f, .08f), At(0, -.14f), stroke, color);
                    Bar(mesh, At(0, -.14f), At(.22f, .08f), stroke, color);
                    break;
            }
        }

        private static void Box(VertexHelper mesh, Vector2 min, Vector2 max, float stroke, Color color) {
            Bar(mesh, new Vector2(min.x, min.y), new Vector2(max.x, min.y), stroke, color);
            Bar(mesh, new Vector2(max.x, min.y), new Vector2(max.x, max.y), stroke, color);
            Bar(mesh, new Vector2(max.x, max.y), new Vector2(min.x, max.y), stroke, color);
            Bar(mesh, new Vector2(min.x, max.y), new Vector2(min.x, min.y), stroke, color);
        }

        private static void Ring(VertexHelper mesh, Vector2 center, float radius, float stroke, Color color) {
            const int segments = 24;
            for (var i = 0; i < segments; i++) {
                var from = Mathf.PI * 2 * i / segments;
                var to   = Mathf.PI * 2 * (i + 1) / segments;
                Bar(mesh, center + new Vector2(Mathf.Cos(from), Mathf.Sin(from)) * radius, center + new Vector2(Mathf.Cos(to), Mathf.Sin(to)) * radius, stroke, color);
            }
        }

        // A stroke from one point to another, its ends squared off past the points so joined strokes meet.
        private static void Bar(VertexHelper mesh, Vector2 from, Vector2 to, float stroke, Color color) {
            var along = (to - from).normalized * stroke;
            var side  = Vector2.Perpendicular(along);
            from -= along;
            to   += along;
            var start = mesh.currentVertCount;
            Vertex(mesh, from - side, color);
            Vertex(mesh, from + side, color);
            Vertex(mesh, to + side, color);
            Vertex(mesh, to - side, color);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }

        private static void Circle(VertexHelper mesh, Vector2 center, float radius, Color color) {
            const int segments = 40;
            var start = mesh.currentVertCount;
            Vertex(mesh, center, color);
            for (var i = 0; i < segments; i++) {
                var angle = Mathf.PI * 2 * i / segments;
                Vertex(mesh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color);
            }

            for (var i = 0; i < segments; i++) mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % segments);
        }

        private static void Vertex(VertexHelper mesh, Vector2 position, Color color) {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color    = color;
            mesh.AddVert(vertex);
        }
    }
}