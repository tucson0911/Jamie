using UnityEngine;

namespace Jamie
{
    public sealed class PlayfieldHud : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        private GUIStyle title;
        private GUIStyle caption;
        private GUIStyle controls;
        private Texture2D panel;

        public void Configure(PlayerController controller) => player = controller;

        private void OnGUI()
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
                title.normal.textColor = new Color(0.91f, 0.88f, 0.74f);
                caption = new GUIStyle(GUI.skin.label) { fontSize = 12 };
                caption.normal.textColor = new Color(0.67f, 0.75f, 0.7f);
                controls = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
                controls.normal.textColor = new Color(0.87f, 0.89f, 0.83f);
                panel = new Texture2D(1, 1);
                panel.SetPixel(0, 0, new Color(0.025f, 0.035f, 0.033f, 0.94f));
                panel.Apply();
            }

            // Keep controls readable at both small editor views and full-screen resolutions.
            float scale = Mathf.Max(0.65f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            GUI.DrawTexture(new Rect(16, 16, 292, 80), panel);
            GUI.Label(new Rect(32, 22, 260, 38), "J A M I E", title);
            GUI.Label(new Rect(33, 61, 260, 25), "TOP-DOWN / PLACEHOLDER PLAYFIELD", caption);
            GUI.DrawTexture(new Rect(width - 198, 16, 182, 44), panel);
            GUI.Label(new Rect(width - 194, 16, 174, 44),
                player != null && player.FlashlightEnabled ? "FLASHLIGHT  ON" : "FLASHLIGHT  OFF", controls);
            GUI.DrawTexture(new Rect(16, height - 58, width - 32, 42), panel);
            GUI.Label(new Rect(24, height - 58, width - 48, 42),
                "WASD / Arrows  Move     |     Mouse  Aim     |     Shift  Sprint     |     F  Flashlight     |     R  Reset", controls);
            GUI.matrix = previous;
        }

        private void OnDestroy()
        {
            if (panel != null)
                Destroy(panel);
        }
    }
}
