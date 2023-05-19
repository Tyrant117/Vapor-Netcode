using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace VaporNetcode.Editor
{
    public class RichStringLogDrawer : OdinValueDrawer<RichStringLog>
    {
        private bool toggle;
        private Vector2 scroll;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            RichStringLog value = this.ValueEntry.SmartValue;
            EditorGUILayout.BeginVertical();
            var hort = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(value.Content, SirenixGUIStyles.RichTextLabel);
            EditorGUILayout.Space();
            if (SirenixEditorGUI.SDFIconButton(hort.AlignRight(24).AlignMiddle(24), SdfIconType.EyeFill, IconAlignment.RightEdge))
            {
                toggle = !toggle;
            }
            EditorGUILayout.EndHorizontal();

            if (toggle)
            {
                var vert = EditorGUILayout.BeginVertical();
                EditorGUILayout.Space();
                if (SirenixEditorGUI.SDFIconButton(vert.AlignRight(24).AlignTop(24), SdfIconType.Folder2Open, IconAlignment.LeftOfText))
                {
                    if (value.FirstFilePath != string.Empty)
                        Task.Run(() => Process.Start(value.FirstFilePath));
                }
                EditorGUILayout.Space(24);
                scroll = EditorGUILayout.BeginScrollView(scroll, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, SirenixGUIStyles.ToggleGroupBackground, GUILayout.MinHeight(128));
                EditorGUILayout.LabelField(value.StackTrace, SirenixGUIStyles.RichTextLabel);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

            }
            EditorGUILayout.EndVertical();
        }
    }
}
