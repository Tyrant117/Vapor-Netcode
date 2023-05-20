using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;
using System.Text;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.Utilities.Editor;
using FuzzySearch = Sirenix.Utilities.Editor.FuzzySearch;
#endif

namespace VaporNetcode
{
    [Serializable]
    public struct RichStringLog : ISearchFilterable
    {
        public string Content;
        public string StackTrace;
        public string FirstFilePath;

        public bool IsMatch(string searchString)
        {
#if UNITY_EDITOR
            return FuzzySearch.Contains(searchString, Content);
#else
            return false;
#endif
        }
    }
    
    [Serializable]
    public class NetLogger
    {

        [TabGroup("Tabs", "Info", Icon = SdfIconType.InfoCircleFill, TabName = "@Info.Count")]
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface), ListDrawerSettings(DraggableItems = false, HideAddButton = true, HideRemoveButton = true, OnTitleBarGUI = "TitleGUIInfo")]
        public List<RichStringLog> Info = new(1000);
        [TabGroup("Tabs", "Warning", Icon = SdfIconType.ExclamationTriangleFill, TabName = "@Warning.Count", TextColor = "yellow")]
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface), ListDrawerSettings(DraggableItems = false, HideAddButton = true, HideRemoveButton = true, OnTitleBarGUI = "TitleGUIWarning")]
        public List<RichStringLog> Warning = new(1000);
        [TabGroup("Tabs", "Error", Icon = SdfIconType.ExclamationOctagonFill, TabName = "@Error.Count", TextColor ="red")]
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface), ListDrawerSettings(DraggableItems = false, HideAddButton = true, HideRemoveButton = true, OnTitleBarGUI = "TitleGUIError")]
        public List<RichStringLog> Error = new(1000);

        private readonly StringBuilder _sb = new();
        private readonly int _infoStraceCount = 5;
        private readonly int _warningStraceCount = 10;
        private readonly int _errorStraceCount = 20;
        private readonly bool _autoClear;

        //private bool _toggleFilter;
        //private int _selected;

        //[Conditional("UNITY_EDITOR")]
        //private void OnTitle()
        //{
        //    _selected = EditorGUILayout.IntPopup(_selected, new string[2] { "[Stats]", "[Items]" }, new int[2] { 1, 2 }, SirenixGUIStyles.DropDownMiniButton, GUILayout.MaxWidth(64));
        //}

        public NetLogger(bool autoClear)
        {
            _autoClear = autoClear;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(LogLevel logLevel, string log)
        {
            if (_autoClear)
            {
                if (Info.Count > 100) { InfoClear(); }
                if (Warning.Count > 100) { WarningClear(); }
                if (Error.Count > 100) { ErrorClear(); }
            }

            string firstFile;
            switch (logLevel)
            {
                case LogLevel.Debug:
                    if (NetLogFilter.LogDebug)
                    {
                        firstFile = _CreateStackTrace(_infoStraceCount);
                        Info.Add(new RichStringLog()
                        {
                            Content = log,
                            StackTrace = _sb.ToString(),
                            FirstFilePath = firstFile,
                        });
                    }
                    break;
                case LogLevel.Info:
                    if (NetLogFilter.LogInfo)
                    {
                        firstFile = _CreateStackTrace(_infoStraceCount);
                        Info.Add(new RichStringLog()
                        {
                            Content = log,
                            StackTrace = _sb.ToString(),
                            FirstFilePath = firstFile,
                        });
                    }
                    break;
                case LogLevel.Warn:
                    if (NetLogFilter.LogWarn)
                    {
                        firstFile = _CreateStackTrace(_warningStraceCount);
                        Warning.Add(new RichStringLog()
                        {
                            Content = log,
                            StackTrace = _sb.ToString(),
                            FirstFilePath = firstFile,
                        });
                    }
                    break;
                case LogLevel.Error:
                    if (NetLogFilter.LogError)
                    {
                        firstFile = _CreateStackTrace(_errorStraceCount);
                        Error.Add(new RichStringLog()
                        {
                            Content = log,
                            StackTrace = _sb.ToString(),
                            FirstFilePath = firstFile,
                        });
                    }
                    break;
                case LogLevel.Fatal:
                    if (NetLogFilter.LogFatal)
                    {
                        firstFile = _CreateStackTrace(_errorStraceCount);
                        Error.Add(new RichStringLog()
                        {
                            Content = log,
                            StackTrace = _sb.ToString(),
                            FirstFilePath = firstFile,
                        });
                    }
                    break;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            string _CreateStackTrace(int traceCount)
            {
                StackTrace t = new(true);
                int count = Mathf.Min(traceCount, t.FrameCount);
                _sb.Clear();
                string firstFile = string.Empty;
                bool isfirst = true;
                for (int i = 0; i < count; i++)
                {
                    var frame = t.GetFrame(i);
                    if (frame.GetFileLineNumber() > 0)
                    {
                        if (isfirst)
                        {
                            firstFile = frame.GetFileName();
                            isfirst = false;
                        }
                        _sb.AppendLine($"<b>{frame.GetMethod().Name}</b> | {frame.GetFileName()} <b>[{frame.GetFileLineNumber()}]</b>");
                    }
                }
                return firstFile;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWithConsole(LogLevel logLevel, string log)
        {
            if (_autoClear)
            {
                if (Info.Count > 100) { InfoClear(); }
                if (Warning.Count > 100) { WarningClear(); }
                if (Error.Count > 100) { ErrorClear(); }
            }

            string firstFile;
            switch (logLevel)
            {
                case LogLevel.Debug:
                    if (NetLogFilter.LogDebug)
                    {
                        firstFile = _CreateStackTrace(_infoStraceCount);
                        Info.Add(new RichStringLog()
                        {
                            Content = log,
                            StackTrace = _sb.ToString(),
                            FirstFilePath = firstFile,
                        });
                    }
                    break;
                case LogLevel.Info:
                    if (NetLogFilter.LogInfo)
                    {
                        firstFile = _CreateStackTrace(_infoStraceCount);
                        Info.Add(new RichStringLog()
                        {
                            Content = log,
                            StackTrace = _sb.ToString(),
                            FirstFilePath = firstFile,
                        });
                    }
                    break;
                case LogLevel.Warn:
                    if (NetLogFilter.LogWarn)
                    {
                        firstFile = _CreateStackTrace(_warningStraceCount);
                        Warning.Add(new RichStringLog()
                        {
                            Content = log,
                            StackTrace = _sb.ToString(),
                            FirstFilePath = firstFile,
                        });
                    }
                    break;
                case LogLevel.Error:
                    if (NetLogFilter.LogError)
                    {
                        firstFile = _CreateStackTrace(_errorStraceCount);
                        Error.Add(new RichStringLog()
                        {
                            Content = log,
                            StackTrace = _sb.ToString(),
                            FirstFilePath = firstFile,
                        });
                    }
                    break;
                case LogLevel.Fatal:
                    if (NetLogFilter.LogFatal)
                    {
                        firstFile = _CreateStackTrace(_errorStraceCount);
                        Error.Add(new RichStringLog()
                        {
                            Content = log,
                            StackTrace = _sb.ToString(),
                            FirstFilePath = firstFile,
                        });
                    }
                    break;
            }

            switch (logLevel)
            {
                case LogLevel.Debug:
                    if (NetLogFilter.LogDebug)
                        Debug.Log(log);
                    break;
                case LogLevel.Info:
                    if (NetLogFilter.LogDebug)
                        Debug.Log(log);
                    break;
                case LogLevel.Warn:
                    if (NetLogFilter.LogWarn)
                        Debug.LogWarning(log);
                    break;
                case LogLevel.Error:
                    if (NetLogFilter.LogError)
                        Debug.LogError(log);
                    break;
                case LogLevel.Fatal:
                    if (NetLogFilter.LogFatal)
                        Debug.LogError(log);
                    break;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            string _CreateStackTrace(int traceCount)
            {
                StackTrace t = new(true);
                int count = Mathf.Min(traceCount, t.FrameCount);
                _sb.Clear();
                string firstFile = string.Empty;
                bool isfirst = true;
                for (int i = 0; i < count; i++)
                {
                    var frame = t.GetFrame(i);
                    if (frame.GetFileLineNumber() > 0)
                    {
                        if (isfirst)
                        {
                            firstFile = frame.GetFileName();
                            isfirst = false;
                        }
                        _sb.AppendLine($"<b>{frame.GetMethod().Name}</b> | {frame.GetFileName()} <b>[{frame.GetFileLineNumber()}]</b>");
                    }
                }
                return firstFile;
            }
        }


        [Conditional("UNITY_EDITOR")]
        private void TitleGUIInfo()
        {
            if (SirenixEditorGUI.ToolbarButton(SdfIconType.XSquare))
            {
                InfoClear();
            }
        }

        [Conditional("UNITY_EDITOR")]
        private void TitleGUIWarning()
        {
            if (SirenixEditorGUI.ToolbarButton(SdfIconType.XSquare))
            {
                WarningClear();
            }
        }

        [Conditional("UNITY_EDITOR")]
        private void TitleGUIError()
        {
            if (SirenixEditorGUI.ToolbarButton(SdfIconType.XSquare))
            {
                ErrorClear();
            }
        }

        private void InfoClear()
        {
            Info.Clear();
        }

        private void WarningClear()
        {
            Warning.Clear();
        }

        private void ErrorClear()
        {
            Error.Clear();
        }
    }
}
