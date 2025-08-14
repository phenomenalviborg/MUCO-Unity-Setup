using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.IO;
using System.Collections.Generic;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using System;
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

        Dictionary<string, string> packages = new Dictionary<string, string>();

        bool isInitialized;
        void Init() {
            if (isInitialized)
                return;

            packages.Add("com.antilatency.sdk", "https://github.com/AntilatencySDK/Release_4.5.0.git#subset-9981b5a2f659d60c5c83913dabf63caeec6c76a7");
            packages.Add("com.antilatency.alt-tracking-xr", "https://github.com/antilatency/Unity.AltTrackingXrPackage.git");
            packages.Add("com.phenomenalviborg.muco","https://github.com/phenomenalviborg/MUCO-Unity.git");
            
            logo = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Packages/com.phenomenalviborg.muco-setup/Editor/MUCO-LOGO.png"
            );
            isInitialized = true;
        }

        void Awake() {
            Init();
            LoadXR();
        }

        void OnValidate() {
            Init();
            LoadXR();   
        }

        Texture2D logo;
        void GuiLine(int i_height = 1)
        {
            GUILayout.Space(5);
            Rect rect = EditorGUILayout.GetControlRect(false, i_height);
            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            GUILayout.Space(5);
        }

        Vector2 scrollPos = Vector2.zero;
    
        private void OnGUI()
        {
            Init();
            LoadXR();
            GUIStyle styleRed = new GUIStyle(GUI.skin.button);
            styleRed.normal.textColor = Color.red;

            GUIStyle styleGreen = new GUIStyle(GUI.skin.button);
            styleGreen.normal.textColor = Color.green;

            GUIStyle styleHeader = new GUIStyle();
            styleHeader.fontSize = 20;
            styleHeader.fontStyle = FontStyle.Bold;
            styleHeader.normal.textColor = Color.white;
            styleHeader.padding = new RectOffset(0, 5, 7, 5);

            GUIStyle styleSubHeader = new GUIStyle();
            styleSubHeader.fontSize = 15;
            styleSubHeader.fontStyle = FontStyle.Bold;
            styleSubHeader.normal.textColor = Color.gray;
            styleSubHeader.padding = new RectOffset(6, 0, 5, 0);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

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
                using (Vertical)
                {
                    foreach (KeyValuePair<string, string> kvp in packages)
                    {
                        if (IsPackageInstalled(kvp.Key))
                        {
                            GUILayout.Label("OK", styleGreen);
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
                        GUILayout.Label("Texture Compression Format: Android Only", styleRed, biggerLineHeight);
                    }
                }
                using (Vertical)
                {
                    if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                    {
                        GUILayout.Label("OK", styleGreen);
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
                            GUILayout.Label("OK", styleGreen);
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
                using (Vertical)
                {

                    if (PlayerSettings.colorSpace == ColorSpace.Linear)
                        GUILayout.Label("OK", styleGreen);
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
                        if (GUILayout.Button("OK", styleGreen))
                        {
                        }
                    }
                }
            }
            GUILayout.Space(20);
            GUILayout.Label("Project Settings - > XR Plug-in Management", styleSubHeader);
            using (Horizontal)
            {
                GUILayout.Label("Target XR Plugins: ");
            }
            using (Horizontal)
            {
                using (Vertical)
                {

                    if (xRGeneralSettings != null)
                    {
                        foreach (XRLoader loader in xRGeneralSettings.Manager.activeLoaders)
                        {

                            GUILayout.Label(loader.name);
                        }
                    }
                }
                using (Vertical)
                {
                    if (xRGeneralSettings)
                        foreach (XRLoader loader in xRGeneralSettings.Manager.activeLoaders)
                        {
                            if (loader.name == "OpenXRLoader")
                            {
                                openXRLoader = loader;
                                GUILayout.Label("OK", styleGreen);
                            }
                            else
                            {
                                if (GUILayout.Button("Remove", styleRed))
                                {
                                    xRGeneralSettings.Manager.TryRemoveLoader(loader);
                                }
                            }
                        }
                }
            }
            if (xRGeneralSettings != null && openXRLoader != null)
            {

            }


            GUILayout.Label("OpenXR -> OpenXR Feature Groups: Hand Tracking Subsystem ON");
            GUILayout.Label("OpenXR -> Latency Optimiziation - Prioritize rendering ON");
            GUILayout.Label("OpenXR -> Multipass ON (For Built-in pipeline)");
            GUILayout.Space(20);
            GUILayout.Label("Headset Build Check List", EditorStyles.boldLabel);
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
                                GUILayout.Label("OK", styleGreen);
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

        XRLoader openXRLoader = null;

        XRGeneralSettingsPerBuildTarget buildTargetSettingsPerBuildTarget;
        XRGeneralSettings xRGeneralSettings;
        
        bool xrLoaded = false;
        private void LoadXR()
        {
            if (xrLoaded)
                return;
            xRGeneralSettings = XRGeneralSettings.CreateInstance<XRGeneralSettings>();

            if (xRGeneralSettings != null)
            {
                buildTargetSettingsPerBuildTarget = null;
                EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettingsPerBuildTarget);
                if (buildTargetSettingsPerBuildTarget == null)
                    return;
                xRGeneralSettings = buildTargetSettingsPerBuildTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
                if (xRGeneralSettings == null)
                    return;
            }
            xrLoaded = true;
        }
        public static bool IsPackageInstalled(string packageId)
        {
            if (!File.Exists("Packages/manifest.json"))
                return false;
            string jsonText = File.ReadAllText("Packages/manifest.json");
            return jsonText.Contains("\""+packageId+"\"");
        }

        public static bool AreAllPAckagesInstalled(Dictionary<string, string> packages) {
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

    }
}
