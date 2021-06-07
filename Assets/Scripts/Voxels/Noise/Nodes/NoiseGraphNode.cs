﻿using XNode;

namespace Tuntenfisch.Voxels.Noise.Nodes
{
    public abstract class NoiseGraphNode : Node
    {
        protected const string c_leafNodeColor = "#B84747";
        protected const string c_internalNodeColor = "#474747";
        protected const string c_rootNodeColor = "#478C47";

        public override object GetValue(NodePort port)
        {
            return null;
        }

        public abstract NodeType GetNodeType();
    }
}