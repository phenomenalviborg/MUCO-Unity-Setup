using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.IO;
using System.Collections.Generic;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using System;
using System.Reflection;
using System.Linq;

namespace Muco
{
    public class MUCOSetup : EditorWindow
    {
        public class GUILayoutHelper : IDisposable
        {
            public enum Orientation
            {
                Horizontal,
                Vertical
            }

            Orientation currentOrientation;

            public GUILayoutHelper(Orientation orientation)
            {
                switch (orientation)
                {
                    case Orientation.Horizontal:
                        GUILayout.BeginHorizontal();
                        break;
                    case Orientation.Vertical:
                        GUILayout.BeginVertical();
                        break;
                }

                this.currentOrientation = orientation;
            }

            public void Dispose()
            {
                switch (currentOrientation)
                {
                    case Orientation.Horizontal:
                        GUILayout.EndHorizontal();
                        break;
                    case Orientation.Vertical:
                        GUILayout.EndVertical();
                        break;
                }
            }
        }

        GUILayoutHelper Horizontal => new GUILayoutHelper(GUILayoutHelper.Orientation.Horizontal);
        GUILayoutHelper Vertical => new GUILayoutHelper(GUILayoutHelper.Orientation.Vertical);

        [MenuItem("MUCO/Setup")]
        public static void ShowWindow()
        {
            MUCOSetup mucoSetup = GetWindow<MUCOSetup>("MUCO Setup");
        }

        Dictionary<string, string> packages = new Dictionary<string, string> {
            { "com.antilatency.sdk", "https://github.com/AntilatencySDK/Release_4.5.0.git#subset-9981b5a2f659d60c5c83913dabf63caeec6c76a7" },
            { "com.antilatency.alt-tracking-xr", "https://github.com/antilatency/Unity.AltTrackingXrPackage.git" },
            { "com.phenomenalviborg.muco","https://github.com/phenomenalviborg/MUCO-Unity.git" }
        };

        bool isInitialized;
        void Init()
        {
            if (isInitialized)
                return;

            logo = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Packages/com.phenomenalviborg.muco-setup/Editor/MUCO-LOGO.png"
            );
            isInitialized = true;
        }

        void Awake()
        {
            Init();
            LoadXR();
        }

        void OnValidate()
        {
            Init();
            LoadXR();
        }

        Texture2D logo;
        void GuiLine(int i_height = 1)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, i_height);
            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        Vector2 scrollPos = Vector2.zero;

        private void OnGUI()
        {
            Init();
            LoadXR();

            GUIStyle styleButtonRed = new GUIStyle(GUI.skin.button);
            styleButtonRed.normal.textColor = Color.red;
            styleButtonRed.fixedWidth = 200;

            GUIStyle styleButtonGreen = new GUIStyle(GUI.skin.button);
            styleButtonGreen.normal.textColor = Color.green;
            styleButtonGreen.fixedWidth = 200;

            GUIStyle styleLabelRed = new GUIStyle();
            styleLabelRed.normal.textColor = Color.red;
            styleLabelRed.padding = new RectOffset(6, 3, 3, 3);

            GUIStyle styleHeader = new GUIStyle();
            styleHeader.fontSize = 20;
            styleHeader.fontStyle = FontStyle.Bold;
            styleHeader.normal.textColor = Color.white;
            styleHeader.padding = new RectOffset(0, 5, 7, 5);

            GUIStyle styleSubHeader = new GUIStyle();
            styleSubHeader.fontSize = 15;
            styleSubHeader.fontStyle = FontStyle.Bold;
            styleSubHeader.normal.textColor = Color.gray;
            styleSubHeader.padding = new RectOffset(6, 0, 0, 0);

            GUIStyle styleBold = new GUIStyle();
            styleBold.fontStyle = FontStyle.Bold;
            styleBold.padding = new RectOffset(6, 0, 0, 0);
            styleBold.normal.textColor = EditorStyles.label.normal.textColor;

            var lineHeight = 19;

            var biggerLineHeight = new GUILayoutOption[] { GUILayout.Height(lineHeight) };
            var labelStyleNextToButton = new GUIStyle(GUI.skin.label);

            GUILayout.Space(5);
            using (Horizontal)
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(false), GUILayout.MaxWidth(26));
                GUILayout.Label(logo, GUILayout.Width(38), GUILayout.Height(38));
                GUILayout.EndVertical();
                using (Vertical)
                {
                    GUILayout.Label("MUCO Unity Setup", styleHeader);
                }
            }
            GuiLine();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            GUILayout.Space(5);
            GUILayout.Label("Required External Packages", styleSubHeader);
            using (Horizontal)
            {
                using (Vertical)
                {
                    foreach (KeyValuePair<string, string> kvp in packages)
                    {
                        GUILayout.Label(kvp.Key, labelStyleNextToButton, biggerLineHeight);
                    }
                }
                GUILayout.FlexibleSpace(); 
                using (Vertical)
                {
                    foreach (KeyValuePair<string, string> kvp in packages)
                    {
                        if (IsPackageInstalled(kvp.Key))
                        {
                            GUILayout.Label("OK", styleButtonGreen);
                        }
                        else
                        {
                            if (GUILayout.Button("Install Package"))
                            {
                                AddPackage(kvp.Key + "@" + kvp.Value);
                            }
                        }
                    }
                    if (!AreAllPAckagesInstalled(packages))
                    {
                        if (GUILayout.Button("Install All Packages"))
                        {
                            AddPackages(packages);
                            Repaint();
                        }
                    }
                }
            }
            GUILayout.Space(20);
            GUILayout.Label("Build Settings", styleSubHeader);
            using (Horizontal)
            {
                using (Vertical)
                {
                    GUILayout.Label("Build Target: " + EditorUserBuildSettings.activeBuildTarget, labelStyleNextToButton, biggerLineHeight);
                    if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                    {
                        string compression = EditorUserBuildSettings.androidBuildSubtarget.ToString();
                        GUILayout.Label("Texture Compression Format: " + compression, labelStyleNextToButton, biggerLineHeight);
                    }
                    else
                    {
                        GUILayout.Label("Texture Compression Format: Android Only", styleButtonRed, biggerLineHeight);
                    }
                }
                GUILayout.FlexibleSpace(); 
                using (Vertical)
                {
                    if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                    {
                        GUILayout.Label("OK", styleButtonGreen);
                    }
                    else
                    {
                        if (GUILayout.Button("Set Build Target to Android"))
                        {
                            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                        }
                    }
                    if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                    {
                        if (EditorUserBuildSettings.androidBuildSubtarget == MobileTextureSubtarget.ASTC)
                        {
                            GUILayout.Label("OK", styleButtonGreen);
                        }
                        else
                        {
                            if (GUILayout.Button("Set to ASTC"))
                            {
                                SetAndroidTextureCompressionToASTC();
                            }
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        GUILayout.Button("Set to ASTC");
                        GUI.enabled = true;
                    }
                }
            }
            GUILayout.Space(20);
            GUILayout.Label("Project Settings - Player", styleSubHeader);
            using (Horizontal)
            {
                using (Vertical)
                {
                    GUILayout.Label("Color Space: " + PlayerSettings.colorSpace);
                    GUILayout.Label("Android Minimum API Level: " + ((int)PlayerSettings.Android.minSdkVersion));
                }
                GUILayout.FlexibleSpace();
                using (Vertical)
                {

                    if (PlayerSettings.colorSpace == ColorSpace.Linear)
                        GUILayout.Label("OK", styleButtonGreen);
                    else
                    {
                        if (GUILayout.Button("Set Color Space to Linear"))
                        {
                            PlayerSettings.colorSpace = ColorSpace.Linear;
                        }
                    }
                    if ((int)PlayerSettings.Android.minSdkVersion < (int)AndroidSdkVersions.AndroidApiLevel32)
                    {
                        if (GUILayout.Button("Set Android API Level to 32"))
                        {
                            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("OK", styleButtonGreen))
                        {
                        }
                    }
                }
            }
            GUILayout.Space(20);
            GUILayout.Label("Project Settings - > Android -> XR Plug-in Management", styleSubHeader);
            bool openXRLoaderFound = false;
            if (xRGeneralSettings != null && xRGeneralSettings.Manager != null)
            {
                foreach (XRLoader loader in xRGeneralSettings.Manager.activeLoaders)
                {
                    using (Horizontal)
                    {
                        
                        if (loader.name == "OpenXRLoader")
                        {
                            openXRLoaderFound = true;
                        }
                        else
                        {
                            
                            using (Vertical)
                            {
                                GUILayout.Label(loader.name + " (Undesired)", styleLabelRed);
                            }
                            GUILayout.FlexibleSpace(); 
                            using (Vertical)
                            {
                                if (GUILayout.Button("Remove", styleButtonRed))
                                {
                                    xRGeneralSettings.Manager.TryRemoveLoader(loader);
                                }
                            }
                        }
                    }
                }
                if (xRGeneralSettings.Manager.activeLoaders.Count <= 1 && openXRLoaderFound)
                {
                    GUILayout.Label("No undesired XR Plugins loaded!");
                }
            }
            using (Horizontal)
            {
                using (Vertical)
                {
                    {
                        GUILayout.Label("OpenXRLoader");
                    }
                }
                GUILayout.FlexibleSpace(); 
                using (Vertical)
                {
                    if (openXRLoaderFound)
                    {
                        if (GUILayout.Button("OK", styleButtonGreen))
                        {
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Use OpenXRLoader", styleButtonRed))
                        {
                            XRPackageMetadataStore.AssignLoader(xRGeneralSettings.Manager, "Unity.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android);
                            EditorUtility.SetDirty(xRGeneralSettings);
                            Reload();
                        }
                    }
                }
            }



            GUILayout.Label("OpenXR -> OpenXR Feature Groups: Hand Tracking Subsystem ON");
            GUILayout.Label("OpenXR -> Latency Optimiziation - Prioritize rendering ON");
            GUILayout.Label("OpenXR -> Multipass ON (For Built-in pipeline)");
            GUILayout.Space(20);
            GUILayout.Label("Headset Build Check List", styleSubHeader);
            GUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            var options = Enum.GetNames(typeof(XRHeadsetType));
            var _selected = EditorGUILayout.Popup("Select Headset", (int)selectedXRHeadsetType, options);
            if (EditorGUI.EndChangeCheck())
            {
                selectedXRHeadsetType = (XRHeadsetType)_selected;
            }
            switch (selectedXRHeadsetType)
            {
                case XRHeadsetType.MetaQuest2:
                    GUILayout.Label("XR Plugin Management -> OpenXR -> Meta Quest Support ON");
                    GUILayout.Label("OpenXR -> Enabled Interaction Profiles: Oculus Touch Controller Profiles");
                    break;
                case XRHeadsetType.Pico4UltraEnterprise:
                    using (Horizontal)
                    {
                        using (Vertical)
                        {
                            GUILayout.Label("Install PicoXR package");
                        }
                        using (Vertical)
                        {
                            if (IsPackageInstalled("com.unity.xr.openxr.picoxr"))
                            {
                                GUILayout.Label("OK", styleButtonGreen);
                            }
                            else
                            {
                                if (GUILayout.Button("Install Package"))
                                {
                                    AddPackage("com.unity.xr.openxr.picoxr@https://github.com/Pico-Developer/PICO-Unity-OpenXR-SDK.git");
                                }
                            }
                        }
                    }
                    GUILayout.Label("XR Plugin Management -> OpenXR -> Pico OpenXR Features ON");
                    GUILayout.Label("XR Plugin Management -> OpenXR -> Pico Support ON");
                    GUILayout.Label("XR Plugin Management -> OpenXR -> Meta Quest Support OFF");
                    GUILayout.Label("XR Plugin Management -> OpenXR -> Enabled Interaction Profiles -> ONLY PICO 4 Ultra Touch Controller Profile ON");
                    break;
            }
            EditorGUILayout.EndScrollView();
        }

        XRHeadsetType selectedXRHeadsetType;

        enum XRHeadsetType
        {
            MetaQuest2,
            Pico4UltraEnterprise
        }

        XRGeneralSettingsPerBuildTarget buildTargetSettingsPerBuildTarget;
        XRGeneralSettings xRGeneralSettings;

        bool xrLoaded = false;
        private void LoadXR()
        {
            if (xrLoaded)
                return;
            buildTargetSettingsPerBuildTarget = null;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettingsPerBuildTarget);
            if (buildTargetSettingsPerBuildTarget == null)
                return;
            xRGeneralSettings = buildTargetSettingsPerBuildTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            if (xRGeneralSettings == null)
                return;
            xrLoaded = true;
        }
        public static bool IsPackageInstalled(string packageId)
        {
            if (!File.Exists("Packages/manifest.json"))
                return false;
            string jsonText = File.ReadAllText("Packages/manifest.json");
            return jsonText.Contains("\"" + packageId + "\"");
        }

        public static bool AreAllPAckagesInstalled(Dictionary<string, string> packages)
        {
            foreach (KeyValuePair<string, string> kvp in packages)
            {
                if (!IsPackageInstalled(kvp.Key))
                {
                    return false;
                }
            }
            return true;
        }

        static void AddPackages(Dictionary<string, string> packages)
        {
            foreach (KeyValuePair<string, string> kvp in packages)
            {
                if (!IsPackageInstalled(kvp.Key))
                    AddPackage(kvp.Key + "@" + kvp.Value);
            }
        }

        static AddRequest Request;
        static void AddPackage(string name)
        {
            Request = Client.Add(name);
        }

        private static void SetAndroidTextureCompressionToASTC()
        {
            if (EditorUserBuildSettings.androidBuildSubtarget != MobileTextureSubtarget.ASTC)
            {
                EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
                Debug.Log("Android texture compression format set to ASTC.");
            }
            else
            {
                Debug.Log("Android texture compression format is already set to ASTC.");
            }
        }

        XRLoader FindOrNewOpenXRLoader()
        {
            Debug.Log("Finding");
            foreach (var loader in xRGeneralSettings.Manager.loaders)
            {
                if (loader == null)
                    continue;
                Debug.Log("hello: " + loader.name);
                if (loader is UnityEngine.XR.OpenXR.OpenXRLoader)
                    return loader;
            }
            var found = FindFirstObjectByType<UnityEngine.XR.OpenXR.OpenXRLoader>();
            if (found != null)
            {
                Debug.Log("I found this");
                return found as XRLoader;
            }
            var newLoader = ScriptableObject.CreateInstance(typeof(UnityEngine.XR.OpenXR.OpenXRLoader)) as XRLoader;
            newLoader.name = "OpenXRLoader";
            return newLoader;
        }
        
        private void Reload()
        {
            var type = Assembly.Load(new AssemblyName("Unity.XR.Management.Editor")).GetType($"UnityEditor.XR.Management.XRManagerSettingsEditor");

            if (type == null) return;

            var xrManagerSettingsEditorInstance = Resources.FindObjectsOfTypeAll<UnityEditor.Editor>().FirstOrDefault(obj => obj.GetType() == type);

            if (xrManagerSettingsEditorInstance == null) return;

            var reloadMethod = type.GetMethod("Reload", BindingFlags.Public | BindingFlags.Instance);

            if (reloadMethod != null)
            {
                reloadMethod.Invoke(xrManagerSettingsEditorInstance, null);
            }
            else
            {
                Debug.LogError("Failed to find the Reload() method.");
            }
        }
    }
}
