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
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using System.IO;

namespace testDLNA
{
  class Program
  {
    private static readonly ManualResetEvent blockEvent =
      new ManualResetEvent(false);
    private static uint cancelHitCount;
    static void Main(string[] args)
    {
      Console.CancelKeyPress += CancelKeyPressed;
      SetupLogging();
      Console.Title = "SimpleDLNA - starting ...";
      var server = new HttpServer(9000);
      var authorizer = new HttpAuthorizer(server);
      var friendlyName = "sdlna";
      server.InfoFormat("Mounting FileServer for {0}", "test");
      var fs = SetupFileServer(DlnaMediaTypes.Image, "test");
      friendlyName = fs.FriendlyName;
      server.RegisterMediaServer(fs);
      server.NoticeFormat("{0} mounted", "test");
      Console.Title = $"{friendlyName} - running ...";
      Run(server);

    }
    private static void Run(HttpServer server)
    {
      server.Info("CTRL-C to terminate");
      blockEvent.WaitOne();

      server.Info("Going down!");
      server.Info("Closed!");
    }
    private static FileServer SetupFileServer(DlnaMediaTypes types, string Tag)
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
      var fs = new FileServer(types, ids, Tag);
       
          fs.FriendlyName = "sdlna";
      
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
        return;
      }
      e.Cancel = true;
      blockEvent.Set();
      LogManager.GetLogger(typeof(Program)).Info("Shutdown requested");
      Console.Title = "SimpleDLNA - shutting down ...";
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
  
