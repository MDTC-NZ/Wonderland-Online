using System.IO;
using Game;
using Plugin;

namespace wlo.pserver.maps
{
    public sealed class NorthIsland60000 : GameMap
    {
        public NorthIsland60000(PluginHost host, FileInfo file)
            : base(host, file)
        {
        }

        public override uint MapID
        {
            get { return 60000; }
        }

        public override string MapName
        {
            get { return "North Island"; }
        }
    }
}
