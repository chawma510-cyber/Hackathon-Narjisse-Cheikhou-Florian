using System;
using UnityEditor.Experimental.GraphView;

namespace unity4dv
{
    public class BaseNode4D : Node
    {
        public string GUID;
        public bool EntyPoint = false;

        public BaseNode4D() : base()
        {
            GUID = Guid.NewGuid().ToString();
            base.capabilities |= Capabilities.Collapsible;
        }

        public Port CreatePortInstance(Direction nodeDirection, Port.Capacity capacity = Port.Capacity.Single)
        {
            return InstantiatePort(Orientation.Horizontal, nodeDirection, capacity, typeof(float));
        }

        public override void OnSelected()
        {
            base.OnSelected();
            BringToFront();
        }
    }

    public class EntryNode : BaseNode4D
    {
        public Port OutputPort;

        public EntryNode() : base()
        {
            title = "START";
            EntyPoint = true;

            var generatedPort = CreatePortInstance(Direction.Output);
            generatedPort.portName = "Next";
            outputContainer.Add(generatedPort);
            OutputPort = generatedPort;

            capabilities &= ~Capabilities.Deletable;
            capabilities |= Capabilities.Renamable;

            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
