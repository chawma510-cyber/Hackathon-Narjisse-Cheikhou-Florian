using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace unity4dv
{
    public class Graph4DWindow : EditorWindow
    {
        private Graph4DView _graphView;
        private Graph4DContainer _container;

        private bool _need_save = false;

        public void LoadFromObject(Graph4DContainer c)
        {
            titleContent.image = AssetDatabase.LoadAssetAtPath<Texture>("Assets/4DViews/4dv_icon2.png");
            _container = c;
            var saveUtility = GraphSaveUtility.GetInstance(_graphView, c);
            saveUtility.LoadGraph();
            EditorApplication.playModeStateChanged += (PlayModeStateChange state) => { if (state == PlayModeStateChange.ExitingEditMode && _need_save) SaveDialogBox(); };
        }

        private void ConstructGraphView()
        {
            _graphView = new Graph4DView(this)
            {
                name = "Graph4D",
            };
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);

            //auto save
            //_graphView.OnGraph4DChanged.AddListener((string s) => { RequestSaveOperation(); });
            //dirty graph
            _graphView.OnGraph4DChanged.AddListener((string s) => { titleContent.text = "Graph 4D *"; _need_save = true; });
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();
            toolbar.Add(new Button(() => RequestSaveOperation()) {text = "Save Graph"});
            rootVisualElement.Add(toolbar);
        }

        private void RequestSaveOperation()
        {
            var saveUtility = GraphSaveUtility.GetInstance(_graphView, _container);
            saveUtility.SaveGraph();
            titleContent.text = "Graph 4D";
            _need_save = false;
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
            GenerateMiniMap();

            rootVisualElement.RegisterCallback<KeyUpEvent>(evt =>
                    {
                    if (evt.modifiers == EventModifiers.Control && evt.keyCode == KeyCode.S)
                        RequestSaveOperation();
                    });
        }

        private void GenerateMiniMap()
        {
            var miniMap = new MiniMap {anchored = true};
            var cords = _graphView.contentViewContainer.WorldToLocal(new Vector2(this.maxSize.x - 10, 30));
            miniMap.SetPosition(new Rect(cords.x, cords.y, 200, 140));
            _graphView.Add(miniMap);
        }

        private void OnDisable()
        {
            if (_need_save) {
                SaveDialogBox();
            }
            rootVisualElement.Remove(_graphView);
        }

        private void SaveDialogBox()
        {
            if (EditorUtility.DisplayDialog("Save Graph 4D", "You have unsaved changes in the Graph 4D. \nWould you like to save the Graph?", "Save", "Cancel")) {
                RequestSaveOperation();
            }
        }

    }
}
