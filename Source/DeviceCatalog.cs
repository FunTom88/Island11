using System;
using System.Collections.Generic;
using System.Linq;
namespace Island11;
internal record AudioDevice(string Id,string Name,bool Connected,string? Container=null,string? Address=null);
internal record DeviceEntry(string Key,string Name,bool? Connected,string Kind="bluetooth",string? Container=null,string? Address=null,AudioDevice? Audio=null);
internal record DeviceSnapshot(IReadOnlyList<DeviceEntry> Devices,IReadOnlyList<string> Errors);
internal static class DeviceCatalog {
 internal static DeviceSnapshot Combine(IEnumerable<DeviceSnapshot> sources){var all=sources.ToArray();return new(Merge(all.SelectMany(s=>s.Devices)),all.SelectMany(s=>s.Errors).ToArray());}
 internal static string? NormalizeAddress(string? address){if(string.IsNullOrWhiteSpace(address))return null;var compact=address.Replace(":","").Replace("-","").Trim();return compact.Length==12&&compact.All(Uri.IsHexDigit)?compact.ToUpperInvariant():null;}
 internal static bool Same(DeviceEntry a,DeviceEntry b)=>a.Key==b.Key||
  (Guid.TryParse(a.Container,out var ac)&&ac!=Guid.Empty&&Guid.TryParse(b.Container,out var bc)&&ac==bc)||
  (NormalizeAddress(a.Address) is string aa&&aa==NormalizeAddress(b.Address));
 internal static List<DeviceEntry> Merge(IEnumerable<DeviceEntry> items){
  var result=new List<DeviceEntry>();
  foreach(var item in items){var index=result.FindIndex(old=>Same(old,item));if(index<0){result.Add(item);continue;}
   var old=result[index];var audio=old.Audio??item.Audio;
   result[index]=old with {Name=string.IsNullOrWhiteSpace(old.Name)?item.Name:old.Name,Connected=audio?.Connected??old.Connected??item.Connected,
    Kind=audio!=null?"headphones":old.Kind=="bluetooth"?item.Kind:old.Kind,Container=old.Container??item.Container,Address=old.Address??item.Address,Audio=audio};
  }
  return result.OrderByDescending(d=>d.Audio!=null).ThenByDescending(d=>d.Connected==true).ThenBy(d=>d.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
 }
 internal static string KindFromName(string name){var s=name.ToLowerInvariant();if(s.Contains("keyboard")||s.Contains("клавиат")||s.Contains("mx keys"))return "keyboard";if(s.Contains("mouse")||s.Contains("мыш")||s.Contains("mx master")||s.Contains("mx anywhere"))return "mouse";if(s.Contains("headphone")||s.Contains("науш")||s.Contains("wh-")||s.Contains("wf-"))return "headphones";return "bluetooth";}
}
