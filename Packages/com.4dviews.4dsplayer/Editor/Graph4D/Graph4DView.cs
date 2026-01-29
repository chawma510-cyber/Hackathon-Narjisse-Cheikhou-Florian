using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace unity4dv
{
    public class Graph4DView : GraphView
    {
        public static readonly Vector2 DefaultNodeSize = new Vector2(200, 150);
        public static readonly Vector2 DefaultCommentBlockSize = new Vector2(300, 200);

        private static readonly string DefaultTransitionName = "New Transition";

        public class Graph4DChanged : UnityEngine.Events.UnityEvent<string> { };
        public Graph4DChanged OnGraph4DChanged = new Graph4DChanged();


        public Graph4DView(Graph4DWindow editorWindow)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Graph4D"));
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            AddElement(new EntryNode());
            graphViewChanged = OnGraphChange;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphView)
            {
                evt.menu.AppendAction("Add Node", e => {
                        var pos = e.eventInfo.localMousePosition;
                        AddElement(CreateNode(viewTransform.matrix.inverse.MultiplyPoint(pos), ""));
                        });
                evt.menu.AppendAction("Add Group Block", e => {
                        var pos = e.eventInfo.localMousePosition;
                        var rect = new Rect(viewTransform.matrix.inverse.MultiplyPoint(pos), DefaultCommentBlockSize);
                        AddElement(CreateCommentBlock(rect));
                        });
            }
            else if (evt.target is Node)
            {
                // Make sure to have an empty menu
                while (evt.menu.MenuItems().Count > 0)
                    evt.menu.RemoveItemAt(0);

                var node = evt.target as Node;
                evt.menu.AppendAction("Delete Node", e => {
                        DeleteSelection();
                        });
            }
            else
                base.BuildContextualMenu(evt);

        }

        public Group CreateCommentBlock(Rect rect, SerializedCommentBlock commentBlockData = null)
        {
            if(commentBlockData is null)
                commentBlockData = new SerializedCommentBlock();
            var group = new Group
            {
                autoUpdateGeometry = true,
                title = commentBlockData.Title
            };
            group.SetPosition(rect);

            group.RegisterCallback<FocusOutEvent>( e=> { OnGraphChange(new GraphViewChange()); });

            return group;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            var startPortView = startPort;

            ports.ForEach((port) =>
            {
                var portView = port;
                if (startPortView != portView && startPortView.node != portView.node)
                    compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        public Node4D CreateNode(Vector2 position, string nodeName)
        {
            var tempNode = new Node4D()
            {
                title = (nodeName == "")? "New State" : nodeName,
            };
            tempNode.OnNodeChanged.AddListener((string node) => { OnGraphChange(new GraphViewChange()); });

            tempNode.SetPosition(new Rect(position, DefaultNodeSize));

            tempNode.Sequence4DField.RegisterValueChangedCallback(evt =>
            {
                tempNode.title = (evt.newValue) ? evt.newValue.name : "Empty State";
                UpdateTransitionsNames(tempNode);
            });

            tempNode.AddTransitionButton.clicked += (
                () => { AddTransition(tempNode); tempNode.Collapse(true); OnGraph4DChanged.Invoke(""); }
            );

            tempNode.OnSelectedNodeChanged.AddListener(UpdatePreview);
            return tempNode;
        }


        public Transition4D AddTransition(Node4D nodeParent, string transitionName = "")
        {
            var tempTransition = new Transition4D(nodeParent);
            tempTransition.Foldout.text = string.IsNullOrEmpty(transitionName) ? DefaultTransitionName : transitionName;

            tempTransition.DeleteButton.clicked += (
                    () => { RemoveTransition(nodeParent, tempTransition); OnGraph4DChanged.Invoke(""); }
            );

            return tempTransition;
        }

        private void RemoveTransition(Node4D node, Transition4D removedTransition)
        {
            foreach (var edge in removedTransition.Port.connections)
            {
                edge.input.Disconnect(edge);
                RemoveElement(edge);
            }

            node.Transitions.RemoveAll(t => (t.PortGUID == removedTransition.PortGUID));

            node.extensionContainer.Remove(removedTransition.PortContainer);
            node.RefreshPorts();
            node.RefreshExpandedState();
        }

        private GraphViewChange OnGraphChange(GraphViewChange change)
        {
            //UnityEngine.Debug.Log("On graph change");
            // Force to remove all edges when removing nodes
            var toRemove = new List<GraphElement>();
            if (change.elementsToRemove != null)
            {
                foreach (Node4D n in change.elementsToRemove.Where(n => n is Node4D))
                {
                    foreach (var t in n.Transitions)
                    {
                        foreach (var e in t.Port.connections)
                            toRemove.Add(e);
                    }
                }
            }

            foreach (var e in toRemove)
                change.elementsToRemove.Add(e);

            // Update transition names when adding or removing edges
            if (change.edgesToCreate != null)
            {
                foreach (var e in change.edgesToCreate)
                    UpdateTransitionsNames(e, false);
            }
            if (change.elementsToRemove != null)
            {
                foreach (Edge e in change.elementsToRemove.Where(e => e is Edge))
                    UpdateTransitionsNames(e, true);
            }

            OnGraph4DChanged.Invoke("");

            return change;
        }

        private void UpdateTransitionsNames(Edge e, bool clearName)
        {
            Transition4D connectedTransition = null;
            foreach (var node in nodes.ToList()/*.Where(n => n is Node4D)*/) //"where" not available in unity 2020
            {
                if (node is Node4D)
                    connectedTransition = ((Node4D)node).Transitions.Find(t => t.Port == e.output);
                if (connectedTransition != null)
                    break;
            }
            if (connectedTransition != null) {
                //var node = nodes.Where(n => n is Node4D).First(n => (n as Node4D).InputPort == e.input);  //"where" not available in unity 2020
                foreach (var node in nodes.ToList())
                {
                    if (node is Node4D && ((Node4D)node).InputPort == e.input)
                        connectedTransition.Foldout.text = clearName ? DefaultTransitionName : ("To "+node.title);
                }
            }
        }

        private void UpdateTransitionsNames(Node4D updatedNode)
        {
            var updatedEdges = updatedNode.InputPort.connections.ToList();
            //foreach (Node4D node in nodes.Where(n => (n is Node4D) && n != updatedNode)) //"where" not available in unity 2020
            foreach (var node in nodes.ToList() )
            {
                if (node is Node4D && (Node4D)node != updatedNode) { 
                    foreach (var t in ((Node4D)node).Transitions)
                    {
                        if (t.Port.connections.Count() == 0) continue;
                        if (updatedEdges.Exists(e => e == t.Port.connections.First()))
                            t.Foldout.text = updatedNode.title;
                    }
                }
            }
        }

        private void UpdatePreview(string guid)
        {
            //foreach (Node4D node in nodes.Where(n => n is Node4D)) //"where" not available in unity 2020
            foreach (var node in nodes.ToList())
                if (node is Node4D)
                    ((Node4D)node).UpdatePreview(guid);
        }

    }
}
