using System;
using System.IO;
using System.Threading;
using System.Windows;
namespace Island11;
public partial class App : Application {
 private Mutex? singleton;
 protected override void OnStartup(StartupEventArgs e) {
  base.OnStartup(e);
  singleton = new Mutex(true, @"Local\Island11.Desktop", out bool first);
  if (!first) { MessageBox.Show("Island11 уже запущен. Заверши старую версию через значок в трее → Выход, затем запусти новую.", "Island11 0.3"); Shutdown(); return; }
  DispatcherUnhandledException += (_, a) => { Log.Write(a.Exception); MessageBox.Show("Ошибка Island11: " + a.Exception.Message, "Island11"); a.Handled = true; };
  MainWindow = new MainWindow(); MainWindow.Show();
 }
 protected override void OnExit(ExitEventArgs e) { singleton?.Dispose(); base.OnExit(e); }
}
internal static class Log {
 internal static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Island11");
 internal static void Write(Exception e) { try { Directory.CreateDirectory(Folder); var p=Path.Combine(Folder,"diagnostics.log"); if(File.Exists(p)&&new FileInfo(p).Length>1024*1024) File.Delete(p); File.AppendAllText(p, DateTime.Now+" "+e+Environment.NewLine); } catch {} }
}
