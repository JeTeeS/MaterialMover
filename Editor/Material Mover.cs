using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using static JeTeeS.MaterialMover.MaterialMoverFunctions;
using static JeTeeS.MaterialMover.MaterialMoverFunctions.SqueezeScope;
using static JeTeeS.MaterialMover.MaterialMoverFunctions.SqueezeScope.SqueezeScopeType;

namespace JeTeeS.MaterialMover
{
    public class MaterialMoverWindow : EditorWindow
    {
        private const string menuPath = "Tools/TES/Material Mover";
        private GameObject selectedGameObject;
        private DefaultAsset moveToFolder;
        private List<Material> selectedMaterials = new List<Material>();
        private Vector2 scrollPosition;

        [MenuItem(menuPath)]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow window = GetWindow(typeof(MaterialMoverWindow), false, "MaterialMover", true);
        }
        
        private void OnGUI()
        {
            selectedGameObject = (GameObject)EditorGUILayout.ObjectField("Selected GameObject:",selectedGameObject,typeof(GameObject),true);
            using (new SqueezeScope((0, 0, Horizontal)))
            {
                moveToFolder = (DefaultAsset)EditorGUILayout.ObjectField("", moveToFolder, typeof(DefaultAsset), false);
                if (GUILayout.Button("Move Mats"))
                {
                    List<UnityEngine.Object> matList = new List<UnityEngine.Object>();
                    foreach (Material selectedMat in selectedMaterials)
                    {
                        Debug.Log(selectedMat.name + " is selected");
                        matList.Add(selectedMat);
                    }
                    MoveAssets(matList, AssetDatabase.GetAssetPath(moveToFolder));
                }
            }
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            using (new SqueezeScope((10, 10, Vertical)))
            {
                if(selectedMaterials.Count() > 0)
                {
                    foreach(Material mat in selectedMaterials.ToArray())
                    {
                        using (new SqueezeScope((0, 0, Horizontal)))
                        {
                            using(new GUIDisableScope())
                            {
                                EditorGUILayout.ObjectField(mat, typeof(Material), false);
                            }
                            if (GUILayout.Button("Remove Material")) selectedMaterials.Remove(mat);
                        }
                    }
                }
            }
            using (new SqueezeScope((10, 10, Vertical)))
            {
                if(selectedGameObject)
                {
                    foreach(Material material in GetAllMeshes(selectedGameObject).FindMats().Distinct())
                    {
                        bool isSelected = false;
                        foreach(Material selectedMat in selectedMaterials) if(selectedMat == material) isSelected = true;
                        if (!isSelected)
                        {
                            using (new SqueezeScope((0, 0, Horizontal)))
                            {
                                using(new GUIDisableScope())
                                {
                                    EditorGUILayout.ObjectField(material, typeof(Material), false);
                                }
                                if (GUILayout.Button("Add Material")) selectedMaterials.Add(material);
                            }
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
    public static class MaterialMoverFunctions
    {
        public static void MoveAssets (List<UnityEngine.Object> assets, string moveTo)
        {
            if(assets.Count() == 0) return;
            if(string.IsNullOrEmpty(moveTo)) return;
            
            foreach(UnityEngine.Object asset in assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                string fileName = System.IO.Path.GetFileName(assetPath);

                AssetDatabase.MoveAsset(assetPath, moveTo + "/" + fileName);
            }
        }
        public static List<Renderer> GetAllMeshes (GameObject obj)
        {
            if (obj != null)
            {
                return obj.GetComponentsInChildren<Renderer>().ToList();
            }
            return null;
        }
        public static List<Material> FindMats (this List<Renderer> renderers)
        {
            List<Material> materials = new List<Material>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    materials.Add(mat);
                }
            }
            return materials;
        }
        public static List<Material> FindMats (this Renderer renderer)
        {
            return new List<Renderer>{ renderer }.FindMats();
        }
        public class GUIDisableScope : IDisposable
        {
            public GUIDisableScope()
            {
                GUI.enabled = false;
            }
            public void Dispose()
            {
                GUI.enabled = true;
            }
            
        }
        public class SqueezeScope : IDisposable
        {
            private readonly SqueezeSettings[] settings;
            public enum SqueezeScopeType
            {
                Horizontal,
                Vertical,
                EditorH,
                EditorV
            }
            public SqueezeScope(SqueezeSettings input) : this(new[] { input })
            {
            }
            public SqueezeScope(params SqueezeSettings[] input)
            {
                settings = input;
                foreach (var squeezeSettings in input)
                {
                    BeginSqueeze(squeezeSettings);
                }
            }
            private void BeginSqueeze(SqueezeSettings squeezeSettings)
            {
                switch (squeezeSettings.type)
                {
                    case Horizontal:
                        GUILayout.BeginHorizontal(squeezeSettings.style);
                        break;
                    case Vertical:
                        GUILayout.BeginVertical(squeezeSettings.style);
                        break;
                    case EditorH:
                        EditorGUILayout.BeginHorizontal(squeezeSettings.style);
                        break;
                    case EditorV:
                        EditorGUILayout.BeginVertical(squeezeSettings.style);
                        break;
                }

                GUILayout.Space(squeezeSettings.width1);
            }
            public void Dispose()
            {
                foreach (var squeezeSettings in settings.Reverse())
                {
                    GUILayout.Space(squeezeSettings.width2);
                    switch (squeezeSettings.type)
                    {
                        case Horizontal:
                            GUILayout.EndHorizontal();
                            break;
                        case Vertical:
                            GUILayout.EndVertical();
                            break;
                        case EditorH:
                            EditorGUILayout.EndHorizontal();
                            break;
                        case EditorV:
                            EditorGUILayout.EndVertical();
                            break;
                    }
                }
            }
        }
        public struct SqueezeSettings
        {
            public int width1;
            public int width2;
            public SqueezeScopeType type;
            public GUIStyle style;

            public static implicit operator SqueezeSettings((int, int) val)
            {
                return new SqueezeSettings { width1 = val.Item1, width2 = val.Item2, type = Horizontal, style = GUIStyle.none };
            }

            public static implicit operator SqueezeSettings((int, int, SqueezeScopeType) val)
            {
                return new SqueezeSettings { width1 = val.Item1, width2 = val.Item2, type = val.Item3, style = GUIStyle.none };
            }

            public static implicit operator SqueezeSettings((int, int, SqueezeScopeType, GUIStyle) val)
            {
                return new SqueezeSettings { width1 = val.Item1, width2 = val.Item2, type = val.Item3, style = val.Item4 };
            }
        }
        //https://forum.unity.com/threads/is-there-a-way-to-input-text-using-a-unity-editor-utility.473743/#post-7191802
        //https://forum.unity.com/threads/is-there-a-way-to-input-text-using-a-unity-editor-utility.473743/#post-7229248
        //Thanks to JelleJurre for help
        public class EditorInputDialog : EditorWindow
        {
            string description, inputText;
            string okButton, cancelButton;
            bool initializedPosition = false;
            Action onOKButton;
            bool shouldClose = false;
            Vector2 maxScreenPos;
            #region OnGUI()
            void OnGUI()
            {
                // Check if Esc/Return have been pressed
                var e = Event.current;
                if (e.type == EventType.KeyDown)
                {
                    switch (e.keyCode)
                    {
                        // Escape pressed
                        case KeyCode.Escape:
                            shouldClose = true;
                            e.Use();
                            break;

                        // Enter pressed
                        case KeyCode.Return:
                        case KeyCode.KeypadEnter:
                            onOKButton?.Invoke();
                            shouldClose = true;
                            e.Use();
                            break;
                    }
                }
                if (shouldClose)
                {  // Close this dialog
                    Close();
                    //return;
                }
                // Draw our control
                var rect = EditorGUILayout.BeginVertical();

                EditorGUILayout.Space(12);
                EditorGUILayout.LabelField(description);

                EditorGUILayout.Space(8);
                GUI.SetNextControlName("inText");
                inputText = EditorGUILayout.TextField("", inputText);
                GUI.FocusControl("inText");   // Focus text field
                EditorGUILayout.Space(12);

                // Draw OK / Cancel buttons
                var r = EditorGUILayout.GetControlRect();
                r.width /= 2;
                if (GUI.Button(r, okButton))
                {
                    onOKButton?.Invoke();
                    shouldClose = true;
                }
                r.x += r.width;
                if (GUI.Button(r, cancelButton))
                {
                    inputText = null;   // Cancel - delete inputText
                    shouldClose = true;
                }
                EditorGUILayout.Space(8);
                EditorGUILayout.EndVertical();

                // Force change size of the window
                if (rect.width != 0 && minSize != rect.size)
                {
                    minSize = maxSize = rect.size;
                }
                // Set dialog position next to mouse position
                if (!initializedPosition && e.type == EventType.Layout)
                {
                    initializedPosition = true;

                    // Move window to a new position. Make sure we're inside visible window
                    var mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                    mousePos.x += 32;
                    if (mousePos.x + position.width > maxScreenPos.x) mousePos.x -= position.width + 64; // Display on left side of mouse
                    if (mousePos.y + position.height > maxScreenPos.y) mousePos.y = maxScreenPos.y - position.height;

                    position = new Rect(mousePos.x, mousePos.y, position.width, position.height);

                    // Focus current window
                    Focus();
                }
            }
            #endregion OnGUI()

            #region Show()
            /// <summary>
            /// Returns text player entered, or null if player cancelled the dialog.
            /// </summary>
            /// <param name="title"></param>
            /// <param name="description"></param>
            /// <param name="inputText"></param>
            /// <param name="okButton"></param>
            /// <param name="cancelButton"></param>
            /// <returns></returns>
            //public static string Show(string title, string description, string inputText, string okButton = "OK", string cancelButton = "Cancel")
            public static void Show(string title, string description, string inputText, Action<string> callBack, string okButton = "OK", string cancelButton = "Cancel")
            {
                // Make sure our popup is always inside parent window, and never offscreen
                // So get caller's window size
                var maxPos = GUIUtility.GUIToScreenPoint(new Vector2(Screen.width, Screen.height));

                if (EditorWindow.HasOpenInstances<EditorInputDialog>())
                    return;

                var window = CreateInstance<EditorInputDialog>();
                window.maxScreenPos = maxPos;
                window.titleContent = new GUIContent(title);
                window.description = description;
                window.inputText = inputText;
                window.okButton = okButton;
                window.cancelButton = cancelButton;
                window.onOKButton += () => callBack(window.inputText);
                window.ShowPopup();
            }
            #endregion Show()
        }
    }
}
