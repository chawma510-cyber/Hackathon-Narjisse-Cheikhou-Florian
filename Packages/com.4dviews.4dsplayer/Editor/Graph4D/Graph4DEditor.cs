using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace unity4dv
{
    [CustomEditor(typeof(Graph4D))]
    public class Graph4DEditor : Editor
    {
        GameObject _gameObject;
        Graph4D _graph4D;

        public override void OnInspectorGUI()
        {
            _graph4D = target as Graph4D;
            _gameObject = _graph4D.gameObject;

            Undo.RecordObject(_graph4D, "Inspector");

            BuildFilesInspector();

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }

        private void BuildFilesInspector()
        {
            _graph4D.PlayOnStart = EditorGUILayout.Toggle(new GUIContent("Play On Start", "Automatically plays the graph when the app starts"), _graph4D.PlayOnStart);

#if UNITY_2021_1_OR_NEWER
        var btn = EditorGUILayout.LinkButton("Edit Graph4D");
#else
            var btn = GUILayout.Button("Edit Graph4D");
#endif

            if (btn)
            {
                if (!_graph4D.Container || _graph4D.Container is null)
                    _graph4D.Container = ScriptableObject.CreateInstance<Graph4DContainer>();

                bool windowIsOpen = EditorWindow.HasOpenInstances<Graph4DWindow>();
                if (!windowIsOpen)
                    EditorWindow.CreateWindow<Graph4DWindow>();
                else
                    EditorWindow.FocusWindowIfItsOpen<Graph4DWindow>();

                var window = EditorWindow.GetWindow<Graph4DWindow>();
                window.titleContent = new GUIContent("Graph 4D");
                window.LoadFromObject(_graph4D.Container);
            }
        }

    }
}
