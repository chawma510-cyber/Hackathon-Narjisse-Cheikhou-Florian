using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

using UnityEngine.UIElements;
using ObjectField = UnityEditor.UIElements.ObjectField;
using Toggle = UnityEngine.UIElements.Toggle;
using TextField = UnityEngine.UIElements.TextField;
using Foldout = UnityEngine.UIElements.Foldout;

namespace unity4dv
{
    public class Node4D : BaseNode4D
    {
        public Port InputPort;
        public ObjectField Sequence4DField;
        public ObjectField LoopSequence4DField;
        public Toggle IsLoopingField;
        public VisualElement LoopContainer;
        public Foldout CollapseToggle;
        public Button AddTransitionButton;

        public List<Transition4D> Transitions = new List<Transition4D>();

#region Events
        public class NodeSelectionChanged : UnityEngine.Events.UnityEvent<string>{};
        public NodeSelectionChanged OnSelectedNodeChanged = new NodeSelectionChanged();
        public class NodeFieldChanged : UnityEngine.Events.UnityEvent<string> { };
        public NodeFieldChanged OnNodeChanged = new NodeFieldChanged();
        #endregion

        public Node4D() : base()
        {
            // style
            capabilities |= Capabilities.Renamable;
            styleSheets.Add(Resources.Load<StyleSheet>("Node4D"));

            // input Port
            InputPort = CreatePortInstance(Direction.Input, Port.Capacity.Multi);
            InputPort.portName = "Input";
            inputContainer.Add(InputPort);

            // sequence 4D section
            extensionContainer.Add(new Label(" 4D Sequence"));
            Sequence4DField = new UnityEditor.UIElements.ObjectField() {
                objectType = typeof(GameObject),
                allowSceneObjects = true,
                tooltip = "Set the Mesh4D object that will be played",
            };
            Sequence4DField.RegisterValueChangedCallback(evt => { OnNodeChanged.Invoke(""); });
            extensionContainer.Add(Sequence4DField);

            IsLoopingField = new Toggle("Loop") { tooltip = "Loop the Sequence 4D if no output has been triggered" };
            extensionContainer.Add(IsLoopingField);

            LoopSequence4DField = new UnityEditor.UIElements.ObjectField() {
                objectType = typeof(GameObject),
                allowSceneObjects = true,
                tooltip = "[OPTIONAL] Set the Mesh4D that will be played as a transition to the current Mesh4D loop (if loop enabled)"
            };
            LoopSequence4DField.RegisterValueChangedCallback(evt => { OnNodeChanged.Invoke(""); });
            LoopContainer = new VisualElement();
            if (IsLoopingField.value)
                LoopContainer.Add(LoopSequence4DField);
            
            IsLoopingField.RegisterValueChangedCallback(evt =>
            {
                LoopSequence4DField.SetEnabled(evt.newValue);
                if (evt.newValue)
                    LoopContainer.Add(LoopSequence4DField);
                else
                    LoopContainer.Remove(LoopSequence4DField);
                OnNodeChanged.Invoke("");
            });
            extensionContainer.Add(LoopContainer);

            // title button section
            titleButtonContainer.Clear();
            CollapseToggle = new Foldout() { value = true};
            CollapseToggle.RegisterValueChangedCallback(evt => { Collapse(evt.newValue); OnNodeChanged.Invoke(""); });
            titleButtonContainer.Add(CollapseToggle);

            AddTransitionButton = new Button() { 
                text = "Add Output", 
                tooltip="Add a new output branch to this node",
            };
            AddTransitionButton.style.backgroundColor = new Color(0.1176471f, 0.2156863f, 0.4235294f, 1f);
            AddTransitionButton.style.borderBottomWidth = AddTransitionButton.style.borderRightWidth = 3;
            extensionContainer.Add(AddTransitionButton);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnSelectedNodeChanged.Invoke(GUID);
        }

        public void UpdatePreview(string selectedGUID)
        {
            var go = Sequence4DField.value as GameObject;
            if (go != null && !Application.isPlaying)
                go.SetActive(GUID == selectedGUID);
        }

        public void Collapse(bool openNode)
        {
            CollapseToggle.value = openNode;
            extensionContainer.style.display = openNode ? DisplayStyle.Flex : DisplayStyle.None;

            if (openNode)
            {
                foreach (var t in Transitions)
                {
                    t.PortContainer.Add(t.Port);
                    t.DeleteButton.style.display = DisplayStyle.Flex;
                }
                outputContainer.Clear();
            } 
            else
            {
                foreach (var t in Transitions)
                {
                    outputContainer.Add(t.Port);
                    t.DeleteButton.style.display = DisplayStyle.None;
                }
            }
            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
