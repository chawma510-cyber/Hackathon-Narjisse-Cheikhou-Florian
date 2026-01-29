using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace unity4dv
{
    public class GraphSaveUtility
    {
        private List<Edge> Edges => _graphView.edges.ToList();
        private List<Node4D> Nodes => _graphView.nodes.ToList().Where(n => n is Node4D).Cast<Node4D>().ToList();
        private EntryNode EntryNode => _graphView.nodes.ToList().First(n => n is EntryNode) as EntryNode;

        private List<Group> CommentBlocks =>
            _graphView.graphElements.ToList().Where(x => x is Group).Cast<Group>().ToList();

        private Graph4DContainer _graphContainer;
        private Graph4DView _graphView;

        public static GraphSaveUtility GetInstance(Graph4DView graphView, Graph4DContainer container)
        {
            return new GraphSaveUtility {
                _graphView = graphView,
               _graphContainer = container,
            };
        }

        public void SaveGraph()
        {
            SaveNodes();
            SaveCommentBlocks();
            SaveViewTransform();
        }

        public void LoadGraph()
        {
            ClearGraph();
            GenerateNodes();
            ConnectNodes();
            GenerateCommentBlocks();
            SetViewTransform();
        }

        private bool SaveNodes()
        {
            _graphContainer.Nodes.Clear();
            var entryNode = EntryNode;
            _graphContainer.EntryNode = new SerializedEntryNode
            {
                Position = entryNode.GetPosition().position,
                LinkedNodeGuid = GetLinkedNodeGUID(entryNode.OutputPort),
            };

            foreach (var node in Nodes.Where(node => !node.EntyPoint))
            {
                var currentSerNode = new SerializedNode4D
                {
                    NodeGUID = node.GUID,
                    Position = node.GetPosition().position,
                    Collapsed = node.CollapseToggle.value,

                    Sequence4D = node.Sequence4DField.value as GameObject,
                    LoopSequence4D = node.LoopSequence4DField.value as GameObject,
                    IsLooping = node.IsLoopingField.value,
                };

                foreach (var transition in node.Transitions)
                {
                    var linkedNode = GetLinkedNodeGUID(transition.Port);
                    currentSerNode.Transitions4D.Add(new SerializedTransition4D
                    {
                        Opened = transition.Foldout.value,
                        LinkedNodeGuid = linkedNode,
                        TransitionName = transition.Foldout.text,
                        TriggerName = transition.TriggerField.value,
                        TransitionSequence4D = transition.TransitionSequence4DField.value as GameObject,
                    });
                }

                int defaultTransitions = currentSerNode.Transitions4D.Where(t => t.TriggerName == "").Count();
                if (defaultTransitions >= 2)
                    Debug.LogWarning("Warning : Having several unnamed trigger outputs on the same node leads to undefined behaviour.");
                else if (currentSerNode.IsLooping && defaultTransitions > 0)
                    Debug.LogWarning("Warning : A node cannot loop and have unnamed trigger output.");

                _graphContainer.Nodes.Add(currentSerNode);
            }

            return true;
        }

        private string GetLinkedNodeGUID(Port port)
        {
            string linkedNode = "";
            if (port.connected)
            {
                var node4d = port.connections.ToArray()[0].input.node as Node4D;
                linkedNode = node4d.GUID;
            }
            return linkedNode;
        }

        private void SaveCommentBlocks()
        {
            _graphContainer.CommentBlock.Clear();
            foreach (var block in CommentBlocks)
            {
                var nodes = block.containedElements.Where(x => x is Node4D).Cast<Node4D>().Select(x => x.GUID)
                    .ToList();

                _graphContainer.CommentBlock.Add(new SerializedCommentBlock
                {
                    ChildNodes = nodes,
                    Title = block.title,
                    Position = block.GetPosition().position
                });
            }
        }

        private void SaveViewTransform()
        {
            var transform = _graphView.viewTransform;
            _graphContainer.Position = transform.position;
            _graphContainer.Scale = transform.scale;
        }

        private void ClearGraph()
        {
            _graphView.RemoveElement(EntryNode);
            foreach (var node in Nodes)
            {
                Edges.Where(x => x.input.node == node).ToList()
                    .ForEach(edge => _graphView.RemoveElement(edge));
                _graphView.RemoveElement(node);
            }
        }

        private void GenerateNodes()
        {
            var entryNode = new EntryNode();
            entryNode.SetPosition(new Rect(_graphContainer.EntryNode.Position, Graph4DView.DefaultNodeSize));
            _graphView.AddElement(entryNode);

            foreach (var serNode in _graphContainer.Nodes)
            {
                var name = (serNode.Sequence4D == null) ? "" : serNode.Sequence4D.name;
                var tempNode = _graphView.CreateNode(serNode.Position, name);
                tempNode.GUID = serNode.NodeGUID;
                tempNode.Sequence4DField.value = serNode.Sequence4D;
                tempNode.LoopSequence4DField.value = serNode.LoopSequence4D;
                tempNode.IsLoopingField.value = serNode.IsLooping;
                if (serNode.IsLooping)
                    tempNode.LoopContainer.Add(tempNode.LoopSequence4DField);

                foreach (var serTransition in serNode.Transitions4D)
                {
                    var newTransition = _graphView.AddTransition(tempNode, serTransition.TransitionName);
                    newTransition.TriggerField.value = serTransition.TriggerName;
                    newTransition.TransitionSequence4DField.value = serTransition.TransitionSequence4D;
                    newTransition.Foldout.value = serTransition.Opened;
                }

                _graphView.AddElement(tempNode);
                tempNode.Collapse(serNode.Collapsed);
            }
        }

        private void ConnectNodes()
        {
            var entryLinkedNode = _graphContainer.EntryNode.LinkedNodeGuid;
            if (!string.IsNullOrEmpty(entryLinkedNode)) 
            {
                var outputNode = Nodes.First(x => x.GUID == entryLinkedNode);
                LinkNodesTogether(EntryNode.OutputPort, outputNode.InputPort);
            }

            foreach (var serNode in _graphContainer.Nodes)
            {
                var inputNode = Nodes.First(x => x.GUID == serNode.NodeGUID);
                for (int i = 0; i<serNode.Transitions4D.Count; ++i)
                {
                    var serTransition = serNode.Transitions4D[i];
                    var transition = inputNode.Transitions[i];

                    if (serTransition.LinkedNodeGuid == "") continue;
                    var outputNode = Nodes.First(x => x.GUID == serTransition.LinkedNodeGuid);
                    LinkNodesTogether(transition.Port, outputNode.InputPort);
                }
            }
        }

        private void LinkNodesTogether(Port outputSocket, Port inputSocket)
        {
            var tempEdge = new Edge()
            {
                output = outputSocket,
                input = inputSocket
            };
            tempEdge?.input.Connect(tempEdge);
            tempEdge?.output.Connect(tempEdge);
            _graphView.Add(tempEdge);
        }

        private void GenerateCommentBlocks()
        {
            foreach (var commentBlock in CommentBlocks)
                _graphView.RemoveElement(commentBlock);

            foreach (var commentBlock in _graphContainer.CommentBlock)
            {
               var block = _graphView.CreateCommentBlock(new Rect(commentBlock.Position, Graph4DView.DefaultCommentBlockSize),
                    commentBlock);
               _graphView.AddElement(block);
               block.AddElements(Nodes.Where(x=>commentBlock.ChildNodes.Contains(x.GUID)));
            }
        }

        private void SetViewTransform()
        {
            _graphView.viewTransform.position = _graphContainer.Position;
            _graphView.viewTransform.scale = _graphContainer.Scale;
        }
    }
}
