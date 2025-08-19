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
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.Assertions;

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

        private const string PrefKey = "MUCOSetupShown";

        [MenuItem("MUCO/Setup")]
        public static void ShowWindow()
        {
            GetWindow<MUCOSetup>("MUCO Setup");
        }

        [InitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            if (!EditorPrefs.GetBool(PrefKey, false))
            {
                EditorApplication.delayCall += () =>
                {
                    ShowWindow();
                    EditorPrefs.SetBool(PrefKey, true);
                };
            }
        }


        Dictionary<string, string> packages = new Dictionary<string, string> {
            { "com.antilatency.sdk", "https://github.com/AntilatencySDK/Release_4.5.0.git#subset-9981b5a2f659d60c5c83913dabf63caeec6c76a7" },
            { "com.antilatency.alt-tracking-xr", "https://github.com/antilatency/Unity.AltTrackingXrPackage.git" },
            { "com.phenomenalviborg.muco","https://github.com/phenomenalviborg/MUCO-Unity.git" }
        };

        

        private Dictionary<string, bool> packageStatusCache = new Dictionary<string, bool>();
        private double lastPackageCheck = 0;
        private const double PACKAGE_CHECK_INTERVAL = 2.0; 

        private OpenXRSettings cachedXRSettings;
        private OpenXRFeature[] cachedXRFeatures;
        private double lastXRCheck = 0;
        private const double XR_CHECK_INTERVAL = 1.0; 

        GUIStyle styleButtonNormal;
        GUIStyle styleButtonRed;
        GUIStyle styleButtonGreen;

        GUIStyle styleLabelRed;
        GUIStyle styleHeader;
        GUIStyle styleSubHeader;
        GUIStyle styleBold;
        GUIStyle styleList;
        
        bool isInitialized;
        void Init()
        {
            Assert.IsNotNull(Event.current, "Init() must be called from OnGUI");
            
            if (isInitialized)
                return;

            logo = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Packages/com.phenomenalviborg.muco-setup/Editor/MUCO-LOGO.png"
            );

            styleButtonNormal = new GUIStyle(GUI.skin.button);
            styleButtonRed = new GUIStyle(GUI.skin.button);
            styleButtonGreen = new GUIStyle(GUI.skin.button);
            styleLabelRed = new GUIStyle();
            styleHeader = new GUIStyle();
            styleSubHeader = new GUIStyle();
            styleBold = new GUIStyle();
            styleList = new GUIStyle();
            styleButtonNormal.fixedWidth = 200;

            styleButtonRed.normal.textColor = Color.red;
            styleButtonRed.fixedWidth = 200;

            styleButtonGreen.normal.textColor = Color.green;
            styleButtonGreen.fixedWidth = 200;

            styleLabelRed.normal.textColor = Color.red;
            styleLabelRed.padding = new RectOffset(6, 3, 3, 3);

            styleHeader.fontSize = 20;
            styleHeader.fontStyle = FontStyle.Bold;
            styleHeader.normal.textColor = Color.white;
            styleHeader.padding = new RectOffset(0, 5, 7, 5);

            styleSubHeader.fontSize = 15;
            styleSubHeader.fontStyle = FontStyle.Bold;
            styleSubHeader.normal.textColor = Color.gray;
            styleSubHeader.padding = new RectOffset(6, 0, 0, 0);

            styleBold.fontStyle = FontStyle.Bold;
            styleBold.padding = new RectOffset(6, 0, 0, 0);
            styleBold.normal.textColor = Color.gray;

            styleList.normal.textColor = EditorStyles.label.normal.textColor;
            styleList.padding = new RectOffset(6, 3, 3, 3);

            isInitialized = true;
        }

        Texture2D logo;
        void GuiLine(int i_height = 1)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, i_height);
            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        Vector2 scrollPos = Vector2.zero;

        private void UpdatePackageStatusCache()
        {
            if (EditorApplication.timeSinceStartup - lastPackageCheck > PACKAGE_CHECK_INTERVAL)
            {
                var oldCache = new Dictionary<string, bool>(packageStatusCache);
                packageStatusCache.Clear();
                foreach (var kvp in packages)
                {
                    packageStatusCache[kvp.Key] = IsPackageInstalledDirect(kvp.Key);
                }
                packageStatusCache["com.unity.xr.openxr.picoxr"] = IsPackageInstalledDirect("com.unity.xr.openxr.picoxr");
                lastPackageCheck = EditorApplication.timeSinceStartup;
                if (!DictionariesEqual(oldCache, packageStatusCache))
                {
                    Repaint();
                }

            }
        }

        private void UpdateXRSettingsCache()
        {
            if (EditorApplication.timeSinceStartup - lastXRCheck > XR_CHECK_INTERVAL)
            {
                cachedXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
                if (cachedXRSettings != null)
                {
                    cachedXRFeatures = cachedXRSettings.GetFeatures<OpenXRFeature>();
                }
                lastXRCheck = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private bool IsPackageInstalledCached(string packageId)
        {
            return packageStatusCache.TryGetValue(packageId, out bool installed) && installed;
        }

        private void OnGUI()
        {
            UpdatePackageStatusCache();
            UpdateXRSettingsCache();
            Init();

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
                        if (EditorGUILayout.LinkButton("Link"))
                        {
                            Application.OpenURL(kvp.Value);
                        }
                    }
                }
                using (Vertical)
                {
                    foreach (KeyValuePair<string, string> kvp in packages)
                    {
                        if (IsPackageInstalledCached(kvp.Key)) 
                        {
                            GUI.enabled = false;
                            GUILayout.Label("OK", styleButtonGreen);
                            GUI.enabled = true;
                        }
                        else
                        {
                            if (GUILayout.Button("Install Package", styleButtonNormal))
                            {
                                AddPackage(kvp.Key + "@" + kvp.Value);
                            }
                        }
                    }
                    if (!AreAllPackagesInstalledCached(packages)) 
                    {
                        if (GUILayout.Button("Install All Packages", styleButtonNormal))
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
                        GUI.enabled = false;
                        GUILayout.Label("Texture Compression Format: Android Only", styleLabelRed, biggerLineHeight);
                        GUI.enabled = true;
                    }
                }
                GUILayout.FlexibleSpace();
                using (Vertical)
                {
                    if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                    {
                        GUI.enabled = false;
                        GUILayout.Label("OK", styleButtonGreen);
                        GUI.enabled = true;
                    }
                    else
                    {
                        if (GUILayout.Button("Set Build Target to Android", styleButtonNormal))
                        {
                            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                        }
                    }
                    if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                    {
                        if (EditorUserBuildSettings.androidBuildSubtarget == MobileTextureSubtarget.ASTC)
                        {
                            GUI.enabled = false;
                            GUILayout.Label("OK", styleButtonGreen);
                            GUI.enabled = true;
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
            GUILayout.Label("Project Settings -> Player", styleSubHeader);
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
                    {
                        GUI.enabled = false;
                        GUILayout.Label("OK", styleButtonGreen);
                        GUI.enabled = true;
                    }
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
                        GUI.enabled = false;
                        GUILayout.Label("OK", styleButtonGreen);
                        GUI.enabled = true;
                    }
                }
            }

            
            GUILayout.Space(20);
            GUILayout.Label("Project Settings ->  XR Plug-in Management -> Android Tab", styleSubHeader);
            GUILayout.Space(5);
            GUILayout.Label("Plug-In Management", styleBold);
            GUILayout.Space(5);

            using (Horizontal)
            {
                using (Vertical)
                {
                    GUILayout.Label("XR Plug-In Management");
                }
                GUILayout.FlexibleSpace();
                using (Vertical)
                {
                    if (!isXRGeneralSettingsPerBuildTargetInitialized())
                    {
                        if (GUILayout.Button("Initalize", styleButtonNormal))
                        {
                            InitializeXRGeneralSettingsPerBuildTarget();
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        GUILayout.Label("OK", styleButtonGreen);
                        GUI.enabled = true;
                    }
                }
            }

            using (Horizontal)
            {
                if (!isXRGeneralSettingsPerBuildTargetInitialized())
                {
                    GUI.enabled = false;
                }
                using (Vertical)
                {
                    GUILayout.Label("Android XR Plug-In Management");
                }
                GUILayout.FlexibleSpace();
                using (Vertical)
                {
                    if (!isXRPluginManagementAndroidInitialized())
                    {
                        if (GUILayout.Button("Initalize", styleButtonNormal))
                        {
                            InitializeAndroidXRSettings();
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        GUILayout.Label("OK", styleButtonGreen);
                        GUI.enabled = true;
                    }
                }
                GUI.enabled = true;
            }

            if (!isXRPluginManagementAndroidInitialized())
                GUI.enabled = false;

            GUILayout.Space(5);
            GUILayout.Label("Plug-In Providers" + (!isXRPluginManagementAndroidInitialized() ? " (Initialize Android Plugin Management)" : ""), styleBold);
            GUILayout.Space(5);

            bool openXRLoaderFound = false;

            if (isXRPluginManagementAndroidInitialized())
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
                using (Horizontal)
                {
                    using (Vertical)
                    {
                        GUILayout.Label("OpenXRLoader");
                    }
                    GUILayout.FlexibleSpace();
                    using (Vertical)
                    {
                        if (openXRLoaderFound)
                        {
                            GUI.enabled = false;
                            GUILayout.Label("OK", styleButtonGreen);
                            GUI.enabled = true;
                        }
                        else
                        {
                            if (GUILayout.Button("Use OpenXRLoader", styleButtonNormal))
                            {
                                Debug.Log(xRGeneralSettings);
                                XRPackageMetadataStore.AssignLoader(xRGeneralSettings.Manager, "Unity.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android);
                                EditorUtility.SetDirty(xRGeneralSettings);
                                Reload();
                            }
                        }
                    }
                }
            }

            GUI.enabled = true;
            GUILayout.Space(5);
            GUILayout.Label("OpenXR Settings", styleBold);
            GUILayout.Space(5);

            
            if (cachedXRSettings != null)
            {
                using (Horizontal)
                {
                    using (Vertical)
                    {
                        GUILayout.Label("Multipass ON (For Built-in pipeline", styleList);
                    }
                    GUILayout.FlexibleSpace();
                    using (Vertical)
                    {
                        if (cachedXRSettings.renderMode == OpenXRSettings.RenderMode.MultiPass)
                        {
                            GUI.enabled = false;
                            GUILayout.Label("OK", styleButtonGreen);
                            GUI.enabled = true;
                        }
                        else
                        {
                            if (GUILayout.Button("Set Render Mode", styleButtonNormal))
                            {
                                cachedXRSettings.renderMode = OpenXRSettings.RenderMode.MultiPass;
                                lastXRCheck = 0;
                            }
                        }
                    }
                }

                using (Horizontal)
                {
                    using (Vertical)
                    {
                        GUILayout.Label("Latency Optimiziation - Prioritize rendering ON", styleList);
                    }
                    GUILayout.FlexibleSpace();
                    using (Vertical)
                    {
                        if (cachedXRSettings.latencyOptimization == OpenXRSettings.LatencyOptimization.PrioritizeRendering)
                        {
                            GUI.enabled = false;
                            GUILayout.Label("OK", styleButtonGreen);
                            GUI.enabled = true;
                        }
                        else
                        {
                            if (GUILayout.Button("Set Latency Optimization", styleButtonNormal))
                            {
                                cachedXRSettings.latencyOptimization = OpenXRSettings.LatencyOptimization.PrioritizeRendering;
                                lastXRCheck = 0; 
                            }
                        }
                    }
                }
            }

            GUI.enabled = true;
            GUILayout.Space(20);
            GUILayout.Label("Headset Build Check List", styleSubHeader);
            GUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            var options = Enum.GetNames(typeof(XRHeadsetType));
            var _selected = EditorGUILayout.Popup("", (int)selectedXRHeadsetType, options);
            if (EditorGUI.EndChangeCheck())
            {
                selectedXRHeadsetType = (XRHeadsetType)_selected;
            }
            GUILayout.Space(5);
            GUILayout.Label("Package", styleBold);
            GUILayout.Space(5);

            switch (selectedXRHeadsetType)
            {
                case XRHeadsetType.Pico4UltraEnterprise:
                    using (Horizontal)
                    {
                        using (Vertical)
                        {
                            GUILayout.Label("Install PicoXR package");
                        }
                        GUILayout.FlexibleSpace();
                        using (Vertical)
                        {
                            if (IsPackageInstalledCached("com.unity.xr.openxr.picoxr"))
                            {
                                GUI.enabled = false;
                                GUILayout.Label("OK", styleButtonGreen);
                                GUI.enabled = true;
                            }
                            else
                            {
                                if (GUILayout.Button("Install Package", styleButtonNormal))
                                {
                                    AddPackage("com.unity.xr.openxr.picoxr@https://github.com/Pico-Developer/PICO-Unity-OpenXR-SDK.git");
                                }
                            }
                        }
                    }
                    break;
            }

            GUILayout.Space(5);
            GUILayout.Label("Features and Profiles", styleBold);
            GUILayout.Space(5);

            if (cachedXRFeatures != null)
            {
                using (Horizontal)
                {
                    using (Vertical)
                    {
                        foreach (var feature in cachedXRFeatures)
                        {
                            if (!OpenXRFeatureShouldBeEnabled(selectedXRHeadsetType, feature.GetType().Name) && !feature.enabled)
                                continue;
                            GUILayout.Label(feature.name, styleList);
                        }
                    }
                    GUILayout.FlexibleSpace();
                    using (Vertical)
                    {
                        bool anywrongfeatures = false;
                        foreach (var feature in cachedXRFeatures)
                        {
                            var shouldBeEnabled = OpenXRFeatureShouldBeEnabled(selectedXRHeadsetType, feature.GetType().Name);

                            if (!shouldBeEnabled && !feature.enabled)
                                continue;

                            if (shouldBeEnabled != feature.enabled)
                                anywrongfeatures = true;

                            if (shouldBeEnabled)
                            {
                                if (feature.enabled)
                                {
                                    GUI.enabled = false;
                                    GUILayout.Label("OK", styleButtonGreen);
                                    GUI.enabled = true;
                                }
                                else
                                {
                                    if (GUILayout.Button("Enable feature", styleButtonNormal))
                                    {
                                        feature.enabled = true;
                                        EditorUtility.SetDirty(cachedXRSettings);
                                        lastXRCheck = 0; 
                                    }
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("Disable feature", styleButtonRed))
                                {
                                    feature.enabled = false;
                                    EditorUtility.SetDirty(cachedXRSettings);
                                    lastXRCheck = 0; 
                                }
                            }
                        }
                        if (anywrongfeatures)
                        {
                            if (GUILayout.Button("Set All Features", styleButtonNormal))
                            {
                                EnableAppropriateFeatures();
                                lastXRCheck = 0; 
                            }
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        XRHeadsetType selectedXRHeadsetType;

        enum XRHeadsetType
        {
            MetaQuest2,
            Pico4UltraEnterprise
        }

        public static XRGeneralSettings xRGeneralSettings;
        public static XRGeneralSettingsPerBuildTarget buildTargetSettingsPerBuildTarget;

        // ... All the other methods stay exactly the same ...
        public static bool isXRGeneralSettingsPerBuildTargetInitialized()
        {
            var i = AssetDatabase.FindAssets("BuildTargetSettings");
            if (i != null)
                return i.Length > 0;
            return false;
        }

        public static bool isXRPluginManagementAndroidInitialized()
        {
            buildTargetSettingsPerBuildTarget = (XRGeneralSettingsPerBuildTarget)AssetDatabase.LoadAssetAtPath("Assets/XR/BuildTargetSettings.asset", typeof(XRGeneralSettingsPerBuildTarget));
            if (buildTargetSettingsPerBuildTarget == null)
                return false;
            xRGeneralSettings = buildTargetSettingsPerBuildTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            return xRGeneralSettings != null;
        }

        public static void InitializeXRGeneralSettingsPerBuildTarget()
        {
            buildTargetSettingsPerBuildTarget = null;
            buildTargetSettingsPerBuildTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(buildTargetSettingsPerBuildTarget, "Assets/XR/BuildTargetSettings.asset");
            xRGeneralSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            XRManagerSettings manager = ScriptableObject.CreateInstance<XRManagerSettings>();
            xRGeneralSettings.AssignedSettings = manager;
            AssetDatabase.AddObjectToAsset(xRGeneralSettings, buildTargetSettingsPerBuildTarget);
            AssetDatabase.AddObjectToAsset(manager, buildTargetSettingsPerBuildTarget);
            buildTargetSettingsPerBuildTarget.SetSettingsForBuildTarget(BuildTargetGroup.Standalone, xRGeneralSettings);
            EditorUtility.SetDirty(buildTargetSettingsPerBuildTarget);
            AssetDatabase.SaveAssets();
        }

        public static void InitializeAndroidXRSettings()
        {
            xRGeneralSettings = null;
            xRGeneralSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            XRManagerSettings manager = ScriptableObject.CreateInstance<XRManagerSettings>();
            xRGeneralSettings.AssignedSettings = manager;
            buildTargetSettingsPerBuildTarget = (XRGeneralSettingsPerBuildTarget)AssetDatabase.LoadAssetAtPath("Assets/XR/BuildTargetSettings.asset", typeof(XRGeneralSettingsPerBuildTarget));
            AssetDatabase.AddObjectToAsset(xRGeneralSettings, buildTargetSettingsPerBuildTarget);
            AssetDatabase.AddObjectToAsset(manager, buildTargetSettingsPerBuildTarget);
            buildTargetSettingsPerBuildTarget.SetSettingsForBuildTarget(BuildTargetGroup.Android, xRGeneralSettings);
            EditorUtility.SetDirty(buildTargetSettingsPerBuildTarget);
            AssetDatabase.SaveAssets();
        }

        
        public static bool IsPackageInstalledDirect(string packageId)
        {
            if (!File.Exists("Packages/manifest.json"))
                return false;
            string jsonText = File.ReadAllText("Packages/manifest.json");
            return jsonText.Contains("\"" + packageId + "\"");
        }

        
        public static bool IsPackageInstalled(string packageId)
        {
            return GetWindow<MUCOSetup>().IsPackageInstalledCached(packageId);
        }

        
        public bool AreAllPackagesInstalledCached(Dictionary<string, string> packages)
        {
            foreach (KeyValuePair<string, string> kvp in packages)
            {
                if (!IsPackageInstalledCached(kvp.Key))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool AreAllPAckagesInstalled(Dictionary<string, string> packages)
        {
            return GetWindow<MUCOSetup>().AreAllPackagesInstalledCached(packages);
        }

        static void AddPackages(Dictionary<string, string> packages)
        {
            foreach (KeyValuePair<string, string> kvp in packages)
            {
                if (!IsPackageInstalledDirect(kvp.Key)) 
                    AddPackage(kvp.Key + "@" + kvp.Value);
            }
        }

        static AddRequest Request;
        static void AddPackage(string name)
        {
            Request = Client.Add(name);
            
            var window = GetWindow<MUCOSetup>();
            window.lastPackageCheck = 0; 
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

        bool OpenXRFeatureShouldBeEnabled(XRHeadsetType headsetType, string featureId)
        {
            if (featureId == "HandTracking")
                return true;
            switch (headsetType)
            {
                case XRHeadsetType.MetaQuest2:
                    if (featureId == "MetaQuestFeature")
                        return true;
                    if (featureId == "OculusTouchControllerProfile")
                        return true;
                    break;
                case XRHeadsetType.Pico4UltraEnterprise:
                    if (featureId == "OpenXRExtensions")
                        return true;
                    if (featureId == "PICOFeature")
                        return true;
                    if (featureId == "PICO4UltraControllerProfile")
                        return true;
                    break;
            }
            return false;
        }

        private void EnableAppropriateFeatures()
        {
            if (cachedXRSettings == null)
            {
                UnityEngine.Debug.Log($"No OpenXR settings found.");
                return;
            }

            Debug.Log(selectedXRHeadsetType);
            if (cachedXRFeatures != null)
            {
                foreach (var feature in cachedXRFeatures)
                {
                    UnityEngine.Debug.Log(feature.GetType().Name + " " + feature.enabled);
                    var shouldBeEnabled = OpenXRFeatureShouldBeEnabled(selectedXRHeadsetType, feature.GetType().Name);
                    feature.enabled = shouldBeEnabled;
                }
                EditorUtility.SetDirty(cachedXRSettings);
            }
        }
    }
}