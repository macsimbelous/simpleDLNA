using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMaier.SimpleDlna.Server;
using NMaier.SimpleDlna.Server.Comparers;
using NMaier.SimpleDlna.Server.Views;
using NMaier.SimpleDlna.Utilities;
using log4net;
using System.Threading;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using System.IO;
using System.Data.SQLite;

namespace Makina
{
  class Program
  {
    private static readonly ManualResetEvent blockEvent =
      new ManualResetEvent(false);
    private static uint cancelHitCount;
    private static SQLiteConnection previews_db;
    static void Main(string[] args)
    {
      if (args.Length <= 0)
      {
        Console.WriteLine("Не заданы теги!");
        return;
      }
      Console.CancelKeyPress += CancelKeyPressed;
      SetupLogging();
      Console.Title = "Makina - starting ...";
      var server = new HttpServer(9000);
      var authorizer = new HttpAuthorizer(server);
      server.InfoFormat("Mounting FileServer for {0}", args[0]);
      var fs = SetupFileServer(DlnaMediaTypes.Image, args);
      server.RegisterMediaServer(fs);
      server.NoticeFormat("{0} mounted", args[0]);
      Console.Title = $"{args[0]} - running ...";
      Run(server);

    }
    private static void Run(HttpServer server)
    {
      server.Info("CTRL-C to terminate");
      blockEvent.WaitOne();

      server.Info("Going down!");
      server.Info("Closed!");
    }
    private static FileServer SetupFileServer(DlnaMediaTypes types, string[] Tags)
    {
      var ids = new Identifiers(
        ComparerRepository.Lookup("title"), true);
      string[] Views = new string[0];
      foreach (var v in Views)
      {
        try
        {
          ids.AddView(v);
        }
        catch (RepositoryLookupException)
        {
          throw new Exception("Invalid view " + v);
        }
      }
      
      var fs = new FileServer(types, ids, Tags.ToList());

      fs.FriendlyName = Tags[0];
      Program.previews_db = new SQLiteConnection("data source=" + "C:\\utils\\erza\\Previews.sqlite");
      Program.previews_db.Open();
      fs.PreviewsDB = Program.previews_db;
      fs.ErzaConnectionString = "data source=C:\\utils\\Erza\\erza.sqlite";
      fs.Load();
      return fs;
    }
    private static void CancelKeyPressed(object sender,
      ConsoleCancelEventArgs e)
    {
      if (cancelHitCount++ == 3)
      {
        LogManager.GetLogger(typeof(Program)).Fatal(
          "Emergency exit commencing");
        Program.previews_db.Close();
        return;
      }
      e.Cancel = true;
      blockEvent.Set();
      LogManager.GetLogger(typeof(Program)).Info("Shutdown requested");
      Console.Title = "Makina - shutting down ...";
      Program.previews_db.Close();
    }
    public static void SetupLogging()
    {
      FileInfo LogFile = new FileInfo(".\\log.txt");
      string LogLevel = "INFO";
    var appender = new ConsoleAppender();
      var layout = new PatternLayout
      {
        ConversionPattern = "%6level [%3thread] %-20.20logger{1} - %message%newline%exception"
      };
      layout.ActivateOptions();
      appender.Layout = layout;
      appender.ActivateOptions();
      if (LogFile != null)
      {
        var fileAppender = new RollingFileAppender
        {
          File = LogFile.FullName,
          Layout = layout,
          MaximumFileSize = "1MB",
          MaxSizeRollBackups = 10,
          RollingStyle = RollingFileAppender.RollingMode.Size,
          ImmediateFlush = false,
          Threshold = Level.Debug
        };
        fileAppender.ActivateOptions();
        BasicConfigurator.Configure(appender, fileAppender);
      }
      else
      {
        BasicConfigurator.Configure(appender);
      }

      var repo = LogManager.GetRepository();
      var level = repo.LevelMap[LogLevel.ToUpperInvariant()];
      if (level == null)
      {
        throw new Exception("Invalid log level");
      }
      repo.Threshold = level;
    }
  }
}
  
