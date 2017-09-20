using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMaier.SimpleDlna.Server;
using NMaier.SimpleDlna.Server.Metadata;

namespace testDLNA
{
  class testFolder : VirtualFolder
  {
    public FileServer Server { get; set; }
    public testFolder()
    {
    }

    public testFolder(IMediaFolder parent, string name)
      : this(parent, name, name)
    {
    }

    public testFolder(IMediaFolder parent, string name, string id)
      : base(parent, name, id)
    {
    }
    public void AddSubFolder(VirtualFolder sub)
    {
      Folders.Add(sub);
    }
  }
}
