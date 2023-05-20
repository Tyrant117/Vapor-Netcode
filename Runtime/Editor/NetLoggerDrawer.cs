using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VaporNetcode.Editor
{
    public class NetLoggerDrawer : OdinValueDrawer<NetLogger>
    {
        [InitializeOnLoadMethod]
        //[System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void Init()
        {
            EditorGUI.hyperLinkClicked -= EditorGUI_hyperLinkClicked;
            EditorGUI.hyperLinkClicked += EditorGUI_hyperLinkClicked;
        }

        private static void EditorGUI_hyperLinkClicked(EditorWindow window, HyperLinkClickedEventArgs args)
        {
            var hyperLinkData = args.hyperLinkData;
            if (hyperLinkData == null || !hyperLinkData.ContainsKey("cs")) { return; }

            var path = hyperLinkData["cs"];
            if (!int.TryParse(hyperLinkData["ln"], out var line))
            {
                line = 1;
            }
            if (!int.TryParse(hyperLinkData["cn"], out var column))
            {
                column = 0;
            }

            var formatFFP = path.Replace('\\', '/');
            if (formatFFP.StartsWith(Application.dataPath))
            {
                var relPath = "Assets" + path.Substring(Application.dataPath.Length);
                var csPath = AssetDatabase.LoadMainAssetAtPath(relPath);
                if (!AssetDatabase.OpenAsset(csPath, line, column))
                {
                    Debug.Log($"Could Not Open File: {formatFFP}");
                }
            }
            else
            {
                Debug.Log($"Could Find File: {formatFFP}");
            }
        }

        private InspectorProperty _info;
        private InspectorProperty _warning;
        private InspectorProperty _error;
        private InspectorProperty _logs;
        private InspectorProperty _combinedLogs;

        private string _search = string.Empty;
        private bool _infoTog = true;
        private bool _warningTog;
        private bool _errorTog;
        private Vector2 _scrollList;
        private Vector2 _scroll;
        //private readonly HashSet<RichStringLog> _openSet = new();
        private RichStringLog _current;

        //
        // Summary:
        //     Rich text label style.
        private static GUIStyle _stackTraceBox;
        public static GUIStyle StackTraceBox
        {
            get
            {
                if (_stackTraceBox == null)
                {
                    _stackTraceBox = new GUIStyle(EditorStyles.textArea)
                    {
                        //richText = true,
                        //wordWrap = false,
                        //fixedHeight = 0,
                        //stretchHeight = true,
                    };
                }

                return _stackTraceBox;
            }
        }

        private static GUIStyle _stackTrace;
        public static GUIStyle StackTrace
        {
            get
            {
                if (_stackTrace == null)
                {
                    _stackTrace = new GUIStyle(EditorStyles.label)
                    {
                        richText = true,
                        wordWrap = false,
                        normal =
                        {
                             textColor = Color.grey,
                        },
                        hover =
                        {
                             textColor = Color.grey,
                        },
                        focused =
                        {
                             textColor = Color.white
                        },
                        active =
                        {
                             textColor = Color.white
                        }
                    };
                }

                return _stackTrace;
            }
        }

        protected override void Initialize()
        {
            _info = this.Property.Children["_infoCount"];
            _warning = this.Property.Children["_warningCount"];
            _error = this.Property.Children["_errorCount"];

            _logs = this.Property.Children["_logs"];
            _combinedLogs = this.Property.Children["Logs"];
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var info = (int)_info.ValueEntry.WeakSmartValue;
            var warning = (int)_warning.ValueEntry.WeakSmartValue;
            var error = (int)_error.ValueEntry.WeakSmartValue;

            var logs = (List<RichStringLog>)_logs.ValueEntry.WeakSmartValue;
            var combinedLogs = (List<RichStringLog>)_combinedLogs.ValueEntry.WeakSmartValue;

            SirenixEditorGUI.BeginHorizontalToolbar(26);
            GUIHelper.PushLabelWidth(40);
            GUILayout.Label(" Logs", SirenixGUIStyles.TitleCentered, GUILayout.ExpandHeight(true));
            GUIHelper.PopLabelWidth();
            if (SirenixEditorGUI.ToolbarButton("Clear"))
            {
                logs.Clear();
                _info.ValueEntry.WeakSmartValue = 0;
                _warning.ValueEntry.WeakSmartValue = 0;
                _error.ValueEntry.WeakSmartValue = 0;
                _current = null;
                GUI.FocusControl(null);
            }
            _search = SirenixEditorGUI.ToolbarSearchField(_search);
            GUILayout.Space(2);
            _infoTog = SirenixEditorGUI.ToolbarToggle(_infoTog, new GUIContent($"{info}", EditorIcons.UnityInfoIcon));
            _warningTog = SirenixEditorGUI.ToolbarToggle(_warningTog, new GUIContent($"{warning}", EditorIcons.UnityWarningIcon));
            _errorTog = SirenixEditorGUI.ToolbarToggle(_errorTog, new GUIContent($"{error}", EditorIcons.UnityErrorIcon));
            SirenixEditorGUI.EndHorizontalToolbar();

            combinedLogs.Clear();
            foreach (var l in logs)
            {
                if (_infoTog && l.Type == 0)
                {
                    if (_search.Length > 0)
                    {
                        if (l.IsMatch(_search))
                        {
                            combinedLogs.Add(l);
                        }
                    }
                    else
                    {
                        combinedLogs.Add(l);
                    }
                }
                if (_warningTog && l.Type == 1)
                {
                    if (_search.Length > 0)
                    {
                        if (l.IsMatch(_search))
                        {
                            combinedLogs.Add(l);
                        }
                    }
                    else
                    {
                        combinedLogs.Add(l);
                    }
                }
                if (_errorTog && l.Type == 2)
                {
                    if (_search.Length > 0)
                    {
                        if (l.IsMatch(_search))
                        {
                            combinedLogs.Add(l);
                        }
                    }
                    else
                    {
                        combinedLogs.Add(l);
                    }
                }
            }

            var rect = SirenixEditorGUI.BeginVerticalList(true, false, GUILayout.MaxHeight(338));
            _scrollList = GUILayout.BeginScrollView(_scrollList);
            foreach (var cl in combinedLogs)
            {
                GUILayout.BeginVertical(GUILayout.MaxHeight(32));
                SirenixEditorGUI.BeginListItem(true, SirenixGUIStyles.ListItem);
                if(_current == cl)
                {
                    GUILayout.BeginHorizontal(EditorStyles.selectionRect);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                }
                var gui = new GUIContent(EditorIcons.UnityInfoIcon);
                switch (cl.Type)
                {
                    case 1:
                        gui = new GUIContent(EditorIcons.UnityWarningIcon);
                        break;
                    case 2:
                        gui = new GUIContent(EditorIcons.UnityErrorIcon);
                        break;
                    default:
                        break;
                }
                GUILayout.Label(gui, GUILayout.MaxWidth(26), GUILayout.ExpandHeight(true));
                EditorGUILayout.SelectableLabel(cl.Content, SirenixGUIStyles.RichTextLabel, GUILayout.ExpandWidth(true));
                if (GUILayout.Button(new GUIContent(EditorIcons.MagnifyingGlass.Highlighted), SirenixGUIStyles.Button, GUILayout.MaxWidth(26), GUILayout.ExpandHeight(true)))
                {
                    _current = _current == cl ? null : cl;
                    GUI.FocusControl(null);
                    //if (!_openSet.Remove(cl))
                    //{
                    //    _openSet.Add(cl);
                    //}
                }
                GUILayout.EndHorizontal();
                //if (_openSet.Contains(cl))
                //{
                //    var width = GUILayoutUtility.GetLastRect();
                //    var min = StackTrace.CalcHeight(new GUIContent(cl.StackTrace), width.width);
                //    GUILayout.BeginVertical(GUILayout.MinHeight(min));
                //    //_scroll = GUILayout.BeginScrollView(_scroll, false, false,
                //    //    GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, SirenixGUIStyles.ToggleGroupBackground,
                //    //    GUILayout.MinHeight(min));
                //    EditorGUILayout.SelectableLabel(cl.StackTrace, StackTrace);
                //    //EditorGUILayout.TextArea(cl.StackTrace, StackTrace);
                //    //GUILayout.TextArea(cl.StackTrace, StackTrace);
                //    //GUILayout.EndScrollView();
                //    GUILayout.EndVertical();
                //}
                SirenixEditorGUI.EndListItem();
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            SirenixEditorGUI.EndVerticalList();

            if (_current != null)
            {
                var width = GUILayoutUtility.GetLastRect();
                var min = StackTraceBox.CalcHeight(new GUIContent(_current.StackTrace), width.width);
                GUILayout.BeginVertical(StackTraceBox, GUILayout.MinHeight(128));
                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(128));
                EditorGUILayout.SelectableLabel(_current.StackTrace, StackTrace, GUILayout.MinHeight(min));
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginVertical(StackTraceBox, GUILayout.MinHeight(128));
                EditorGUILayout.SelectableLabel("", GUILayout.MinHeight(128));
                GUILayout.EndVertical();
            }
        }
    }
}
