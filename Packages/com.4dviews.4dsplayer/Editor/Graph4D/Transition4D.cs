using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

using ObjectField = UnityEditor.UIElements.ObjectField;
using TextField = UnityEngine.UIElements.TextField;
using Foldout = UnityEngine.UIElements.Foldout;
using Box = UnityEngine.UIElements.Box;
using Button = UnityEngine.UIElements.Button;
using Label = UnityEngine.UIElements.Label;
using FlexDirection = UnityEngine.UIElements.FlexDirection;
using Justify = UnityEngine.UIElements.Justify;
using UnityEngine.UIElements;

namespace unity4dv
{
    public class Transition4D
    {
        public Foldout Foldout;
        public Box PortContainer;
        public Button DeleteButton;
        public string PortGUID;
        public Port Port;
        public TextField TriggerField;
        public ObjectField TransitionSequence4DField;

        public Transition4D(Node4D node)
        {
            // style
            PortContainer = new Box();
            PortContainer.style.flexDirection = FlexDirection.Row;
            PortContainer.style.justifyContent = Justify.SpaceBetween;
            PortContainer.style.borderTopColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            //PortContainer.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 1f);

            Foldout = new Foldout() { value = false };
            PortContainer.Add(Foldout);

            // create port
            PortGUID = Guid.NewGuid().ToString();
            Port = node.CreatePortInstance(Direction.Output);
            Port.portName = "";
            Port.tooltip = "Output Branch";
            PortContainer.Add(Port);

            // delete button
            DeleteButton = new Button() {text = "-" };
            Port.Add(DeleteButton);

            // trigger section
            var triggerContainer = new UnityEngine.UIElements.VisualElement();
            triggerContainer.style.flexDirection = FlexDirection.Row;
            triggerContainer.style.justifyContent = Justify.SpaceBetween;

            triggerContainer.Add(new Label("Trigger : ") { tooltip= "name string to use in script to trigger this output \nWARNING if no trigger name is defined, it will be used as the default activated output"});

            TriggerField = new TextField() { name = string.Empty, value = "",  tooltip = "name string to use in script to trigger this output \nWARNING if no trigger name is defined, it will be used as the default activated output"  };
            TriggerField.RegisterValueChangedCallback(evt => { node.OnNodeChanged.Invoke(""); });
            var containerTriggerNameField = new UnityEngine.UIElements.VisualElement();
            containerTriggerNameField.Add(TriggerField);
            containerTriggerNameField.style.minWidth = 75;
            containerTriggerNameField.style.minWidth = 100;
            triggerContainer.Add(containerTriggerNameField);

            Foldout.Add(triggerContainer);

            // transition sequence section
            TransitionSequence4DField = new UnityEditor.UIElements.ObjectField()
            {
                objectType = typeof(GameObject),
                allowSceneObjects = true,
                tooltip = "[OPTIONAL] Mesh4D that will be played as transition to the next node"
            };
            TransitionSequence4DField.RegisterValueChangedCallback(evt => { node.OnNodeChanged.Invoke(""); });

            Foldout.Add(new Label("Transition Sequence :") { tooltip= "[OPTIONAL] Mesh4D that will be played as transition to the next node" });
            Foldout.Add(TransitionSequence4DField);

            // node update
            node.extensionContainer.Add(PortContainer);
            node.RefreshPorts();
            node.RefreshExpandedState();
            node.Transitions.Add(this);
        }
    }
}
