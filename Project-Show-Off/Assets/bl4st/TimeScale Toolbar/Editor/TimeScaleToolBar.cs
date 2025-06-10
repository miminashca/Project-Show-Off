using UnityEditor;
using UnityEngine;
using System;
using UnityToolbarExtender;

namespace bl4st.TimeScaleToolbar
{
    [InitializeOnLoad]
    public class TimeScaleToolbar
    {
        static readonly string key_enabled = "TimeScaleToolbar_Enabled";
        static readonly string key_forcedOverride = "TimeScaleToolbar_ForcedOverride";
        static readonly string key_timeScale = "TimeScaleToolbar_TimeScale";
        static readonly string key_maxScale = "TimeScaleToolbar_Max";
        static readonly string key_toolbarPosition = "TimeScaleToolbar_Position";
        static readonly string key_toolbarOffset = "TimeScaleToolbar_Offset";
        static readonly string[] toolbarPositions = { "Left", "Right" };

        static float _maxScale = 2f;
        static float maxScale
        {
            get => _maxScale;
            set
            {
                if (_maxScale != value)
                {
                    _maxScale = value;
                    if (_maxScale < timeScale)
                        timeScale = _maxScale;
                    EditorPrefs.SetFloat(key_maxScale, _maxScale);
                }
            }
        }

        static int _toolbarPosition = 1;
        static int toolbarPosition
        {
            get => _toolbarPosition;
            set
            {
                if (_toolbarPosition != value)
                {
                    _toolbarPosition = value;
                    EditorPrefs.SetInt(key_toolbarPosition, _toolbarPosition);
                }
            }
        }


        static int _toolbarOffset = 10;
        static int toolbarOffset
        {
            get => _toolbarOffset;
            set
            {
                if (_toolbarOffset != value)
                {
                    _toolbarOffset = value;
                    EditorPrefs.SetInt(key_toolbarOffset, _toolbarOffset);
                }
            }
        }


        static bool enabled = true;

        static bool _forcedOverride = false;
        static bool forcedOverride
        {
            get => _forcedOverride;
            set
            {
                if (_forcedOverride != value)
                {
                    _forcedOverride = value;
                    EditorPrefs.SetBool(key_forcedOverride, _forcedOverride);
                }
            }
        }

        static float _timeScale = 1f;
        static float timeScale
        {
            get => _timeScale;
            set
            {
                if (_timeScale != value)
                {
                    _timeScale = value;
                    EditorPrefs.SetFloat(key_timeScale, _timeScale);
                }
            }
        }



        static readonly float _sliderWidth = 150f;


        static TimeScaleToolbar()
        {
            enabled = EditorPrefs.GetBool(key_enabled, true);
            EditorPrefs.SetBool(key_enabled, enabled);

            forcedOverride = EditorPrefs.GetBool(key_forcedOverride, false);
            maxScale = EditorPrefs.GetFloat(key_maxScale, 2f);
            toolbarPosition = EditorPrefs.GetInt(key_toolbarPosition, 1);
            toolbarOffset = EditorPrefs.GetInt(key_toolbarOffset, 10);
            timeScale = Mathf.Min(EditorPrefs.GetFloat(key_timeScale, 1f), maxScale);
            if(enabled)
                SetVisibility();

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void UpdateDrawCallbacks()
        {
            if (toolbarPosition == 0)
            {
                ToolbarExtender.RightToolbarGUI.Remove(OnToolbarGUI);
                ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
            }
            else
            {
                ToolbarExtender.LeftToolbarGUI.Remove(OnToolbarGUI);
                ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
            }
        }

        static void SetVisibility()
        {
            if (enabled)
                UpdateDrawCallbacks();
            else
            {
                ToolbarExtender.LeftToolbarGUI.Remove(OnToolbarGUI);
                ToolbarExtender.RightToolbarGUI.Remove(OnToolbarGUI);
            }
            SceneView.RepaintAll();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (enabled)
                    Time.timeScale = timeScale;
                else
                    Time.timeScale = 1f;
            }
        }

        static GUIContent forcedOverrideToggleContent = new GUIContent("", "Toggles forced timeScale override");
        static void OnToolbarGUI()
        {
            GUILayout.Space(toolbarOffset);
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(1));

            GUILayout.Label("TimeScale", GUILayout.Width(70));

            if (Time.timeScale != timeScale)
            {
                if (forcedOverride)
                    Time.timeScale = timeScale;
                else
                    timeScale = Time.timeScale;
            }



            timeScale = GUILayout.HorizontalSlider(timeScale, 0f, maxScale, GUILayout.Width(_sliderWidth));

            GUILayout.Space(4);
            GUILayout.Label(timeScale.ToString("F2"), GUILayout.Width(45));
            GUILayout.Space(-12);
            forcedOverride = GUILayout.Toggle(forcedOverride, forcedOverrideToggleContent);

            if (GUILayout.Button("Reset", GUILayout.Width(50)))
            {
                timeScale = 1f;
                Time.timeScale = timeScale;
            }

            GUILayout.EndHorizontal();

            if (Time.timeScale != timeScale)
                Time.timeScale = timeScale;
        }

        static GUIContent forcedOverrideSettingsContent = new GUIContent("Forced Override", "Toggles forced timeScale override");
        static GUIContent enabledSettingsContent = new GUIContent("Enabled", "Toggles toolbar visibility and behaviour");

        [SettingsProvider]
        public static SettingsProvider MyTimeScaleToolbarSettingsProvider()
        {
            var provider = new SettingsProvider("Project/TimeScaleToolbarSettingsProvider", SettingsScope.Project)
            {
               
                label = "TimeScale Toolbar",
                guiHandler = (searchContext) =>
                {
                    bool oldEnabled = enabled;
                    enabled = EditorGUILayout.Toggle(enabledSettingsContent, enabled);
                    if(enabled != oldEnabled)
                    {
                        SetVisibility();
                        EditorPrefs.SetBool(key_enabled, enabled);
                        if(!enabled)
                            Time.timeScale = 1f;
                        else
                            Time.timeScale = timeScale;
                    }


                    forcedOverride = EditorGUILayout.Toggle(forcedOverrideSettingsContent, forcedOverride);

                    int oldPos = toolbarPosition;
                    toolbarPosition = EditorGUILayout.Popup("Toolbar Position", toolbarPosition, toolbarPositions);
                    if (oldPos != toolbarPosition && enabled)
                        UpdateDrawCallbacks();

                    toolbarOffset = EditorGUILayout.IntSlider("Position Offset", toolbarOffset, 0, Screen.width);


                    maxScale = Mathf.Clamp(EditorGUILayout.FloatField("Maximum TimeScale", maxScale), 1f, 100f);


                    GUIStyle italicStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontStyle = FontStyle.Italic
                    };

                    EditorGUILayout.Space(15);
                    EditorGUILayout.LabelField("If the toolbar is not properly updated, press any key", italicStyle);
                    EditorGUILayout.LabelField("or hover the mouse above the toolbar", italicStyle);
                }
            };
            return provider;
        }
    }




}
