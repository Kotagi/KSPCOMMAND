using UnityEngine;

namespace KspWebMap
{
    public sealed class HelloWorldWindow : MonoBehaviour
    {
        private const string LogPrefix = "[KspWebMap]";
        private readonly int _windowId = typeof(HelloWorldWindow).FullName.GetHashCode();

        private Rect _windowRect = new Rect(220f, 120f, 320f, 150f);
        private bool _hasSaidHello;
        private int _clickCount;

        private void OnGUI()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }

            _windowRect = GUILayout.Window(
                _windowId,
                _windowRect,
                DrawWindow,
                "KSP Web Map - Dev Test");
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Label("Plugin status: loaded in flight scene.");

            if (GUILayout.Button("Say Hello World"))
            {
                _clickCount++;
                _hasSaidHello = true;

                Debug.Log(string.Format("{0} Hello World button clicked. Count: {1}", LogPrefix, _clickCount));
                ScreenMessages.PostScreenMessage(
                    "Hello World from KSP Web Map!",
                    4f,
                    ScreenMessageStyle.UPPER_CENTER);
            }

            if (_hasSaidHello)
            {
                GUILayout.Label(string.Format("Hello confirmed. Click count: {0}", _clickCount));
            }
            else
            {
                GUILayout.Label("Press the button to verify mod interaction.");
            }

            GUI.DragWindow();
        }
    }
}
