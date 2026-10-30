using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
namespace Island11;
internal sealed class Settings {
 public double CenterX {get;set;} = double.NaN;
 public double BottomY {get;set;} = double.NaN;
 public string MediaSource {get;set;} = "";
 static string FileName => Path.Combine(Log.Folder,"settings.json");
 public static Settings Load() { try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FileName)) ?? new(); } catch { return new(); } }
 public void Save() { try { Directory.CreateDirectory(Log.Folder); File.WriteAllText(FileName,JsonSerializer.Serialize(this)); } catch(Exception e) {Log.Write(e);} }
 public static bool AutoStart { get { using var k=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"); return k?.GetValue("Island11") != null; } }
 public static void SetAutoStart(bool enabled) { using var k=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"); if(enabled) k.SetValue("Island11", "\""+Environment.ProcessPath+"\""); else k.DeleteValue("Island11",false); }
}
