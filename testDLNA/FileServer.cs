using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMaier.SimpleDlna.Server;
using System.Data.SQLite;
using System.IO;

namespace Makina
{
  class FileServer : IMediaServer
  {
    private readonly Identifiers ids;
    private readonly DlnaMediaTypes types;
    private object parent;
    private string Tag;
    public SQLiteConnection PreviewsDB;
    public string ErzaConnectionString;
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
      this.Tag = Tag;
      //UUID = DeriveUUID();
    }
    public void Load()
    {
      DoRoot();
    }
    private void DoRoot()
    {
      MakinaFolder newMaster = new MakinaFolder(null, FriendlyName);
      newMaster.Server = this;
      newMaster.Id = Identifiers.GENERAL_ROOT;
      
      MakinaFolder subdir = new MakinaFolder(newMaster, FriendlyName);
      subdir.Server = this;
      newMaster.AddSubFolder(subdir);
      FileInfo[] files = GetFilesByTag(this.Tag);
      foreach (FileInfo file in files)
      {
        var t = GetFile(subdir, file);
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
    internal BaseFile GetFile(MakinaFolder aParent, System.IO.FileInfo info)
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
      DlnaMime type;
      DlnaMediaTypes mediaType;
      if (ext == "BMP")
      {
        type = DlnaMaps.Ext2Dlna["JPG"];
        mediaType = DlnaMaps.Ext2Media["JPG"];
      }
      else
      {
        type = DlnaMaps.Ext2Dlna[ext];
        mediaType = DlnaMaps.Ext2Media[ext];
      }
      
      //var mediaType = DlnaMaps.Ext2Media[ext];
      var rv = BaseFile.GetFile(aParent, info, type, mediaType);
      //pendingFiles.Enqueue(new WeakReference(rv));
      return rv;
    }
    internal Cover GetCover(BaseFile file)
    {
      Stream prev = GetPreview(Path.GetFileNameWithoutExtension(file.Path));
      if(prev != null)
      {
        Cover cover = new Cover(new System.IO.FileInfo(file.Path), prev);
        prev.Close();
        return cover;
      }
      return new Cover(new System.IO.FileInfo(file.Path));
    }
    private FileInfo[] GetFilesByTag(string Tag)
    {
      using (SQLiteConnection connection = new SQLiteConnection(ErzaConnectionString))
      {
        connection.Open();
        List<FileInfo> imgs = new List<FileInfo>();
        string sql = "select images.image_id, images.hash, images.is_deleted, images.width, images.height, images.file_path from tags inner join image_tags on tags.tag_id = image_tags.tag_id inner join images on images.image_id = image_tags.image_id where tags.tag = @tag AND images.is_deleted = 0;";
        using (SQLiteCommand command = new SQLiteCommand(sql, connection))
        {
          command.Parameters.AddWithValue("tag", Tag);
          SQLiteDataReader reader = command.ExecuteReader();
          while (reader.Read())
          {
            object o = reader["file_path"];
            if (o != DBNull.Value)
            {
              FileInfo fi = new FileInfo((string)o);
              if (fi.Exists)
              {
                imgs.Add(fi);
              }
            }
          }
          reader.Close();
          return imgs.ToArray();
        }
      }
    }
    private Stream GetPreview(string hash)
    {
      using (SQLiteCommand command = new SQLiteCommand(PreviewsDB))
      {
        command.CommandText = "SELECT preview FROM previews WHERE hash = @hash;";
        command.Parameters.AddWithValue("hash", hash);
        byte[] tmp = (byte[])command.ExecuteScalar();
        if (tmp != null)
        {
          return new MemoryStream(tmp);
        }
        else
        {
          return null;
        }
      }
    }
  }
}
