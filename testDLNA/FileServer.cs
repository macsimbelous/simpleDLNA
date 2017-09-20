using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMaier.SimpleDlna.Server;

namespace testDLNA
{
  class FileServer : IMediaServer
  {
    private readonly Identifiers ids;
    private readonly DlnaMediaTypes types;
    private object parent;

    public IHttpAuthorizationMethod Authorizer { get; set; }

    public string FriendlyName { get; set; }

    // ReSharper disable once MemberInitializerValueIgnored
    public Guid UUID { get; } = Guid.NewGuid();

    public IMediaItem GetItem(string id)
    {
      lock (ids)
      {
        return ids.GetItemById(id);
      }
    }
    public FileServer(DlnaMediaTypes types, Identifiers ids, string Tag)
    {
      this.types = types;
      this.ids = ids;
      FriendlyName = Tag;
      //UUID = DeriveUUID();
    }
    public void Load()
    {
      DoRoot();
    }
    private void DoRoot()
    {
      testFolder newMaster = new testFolder(null, FriendlyName);
      newMaster.Server = this;
      newMaster.Id = Identifiers.GENERAL_ROOT;
      
      testFolder subdir = new testFolder(newMaster, FriendlyName);
      subdir.Server = this;
      newMaster.AddSubFolder(subdir);
      string[] files = System.IO.Directory.GetFiles(@"I:\Wallpapers\New_Year", "*.jpg");
      foreach(string file in files)
      {
        var t = GetFile(subdir, new System.IO.FileInfo(file));
        subdir.AddResource(t);
      }
      RegisterNewMaster(newMaster);
      //Thumbnail();
    }
    private void RegisterNewMaster(IMediaFolder newMaster)
    {
      lock (ids)
      {
        ids.RegisterFolder(Identifiers.GENERAL_ROOT, newMaster);
        ids.RegisterFolder(
          Identifiers.SAMSUNG_IMAGES,
          new VirtualClonedFolder(
            newMaster,
            Identifiers.SAMSUNG_IMAGES,
            types & DlnaMediaTypes.Image
            )
          );
        ids.RegisterFolder(
          Identifiers.SAMSUNG_AUDIO,
          new VirtualClonedFolder(
            newMaster,
            Identifiers.SAMSUNG_AUDIO,
            types & DlnaMediaTypes.Audio
            )
          );
        ids.RegisterFolder(
          Identifiers.SAMSUNG_VIDEO,
          new VirtualClonedFolder(
            newMaster,
            Identifiers.SAMSUNG_VIDEO,
            types & DlnaMediaTypes.Video
            )
          );
      }
    }
    internal BaseFile GetFile(testFolder aParent, System.IO.FileInfo info)
    {
      BaseFile item;
      lock (ids)
      {
        item = ids.GetItemByPath(info.FullName) as BaseFile;
      }
      if (item != null &&
          item.InfoDate == info.LastWriteTimeUtc &&
          item.InfoSize == info.Length)
      {
        return item;
      }

      string ext = System.IO.Path.GetExtension(info.FullName).Replace(".", String.Empty).ToUpper();
      var type = DlnaMaps.Ext2Dlna[ext];
      var mediaType = DlnaMaps.Ext2Media[ext];
      var rv = BaseFile.GetFile(aParent, info, type, mediaType);
      //pendingFiles.Enqueue(new WeakReference(rv));
      return rv;
    }
    internal Cover GetCover(BaseFile file)
    {
      return new Cover(new System.IO.FileInfo(file.Path));
    }
  }
}
