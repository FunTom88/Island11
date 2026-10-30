using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
namespace Island11;
internal sealed class DeviceService {
 sealed record Source(string Name,IReadOnlyList<DeviceEntry> Rows,string? Error=null);
 readonly Dictionary<string,Task<Source>> running=new();
 public async Task<DeviceSnapshot> Read(){
  var results=await Task.WhenAll(
   ReadSource("Аудиодрайвер",()=>Task.FromResult(BluetoothAudio.List().Select(d=>new DeviceEntry("audio:"+d.Id,d.Name,d.Connected,"headphones",d.Container,d.Address,d)).ToList())),
   ReadSource("Bluetooth Classic",()=>Task.FromResult(NativeBluetooth.List())),
   ReadSource("Bluetooth Windows",()=>ReadWindows(false)),
   ReadSource("Bluetooth LE",()=>ReadWindows(true)));
  return DeviceCatalog.Combine(results.Select(r=>new DeviceSnapshot(r.Rows,r.Error==null?Array.Empty<string>():new[]{r.Name+": "+r.Error})));
 }
 async Task<Source> ReadSource(string name,Func<Task<List<DeviceEntry>>> load){
  if(!running.TryGetValue(name,out var task)||task.IsCompleted){task=Task.Run(async()=>{try{return new Source(name,await load());}catch(Exception e){Log.Write(new Exception("Устройства / "+name,e));return new Source(name,Array.Empty<DeviceEntry>(),e.GetType().Name+": "+e.Message);}});running[name]=task;}
  try{return await task.WaitAsync(TimeSpan.FromSeconds(9));}catch(TimeoutException){return new Source(name,Array.Empty<DeviceEntry>(),"превышено время ожидания");}
 }
 static async Task<List<DeviceEntry>> ReadWindows(bool le){
  string selector=le?BluetoothLEDevice.GetDeviceSelectorFromPairingState(true):BluetoothDevice.GetDeviceSelectorFromPairingState(true);
  string[] properties={"System.Devices.Aep.IsConnected","System.Devices.Aep.DeviceAddress","System.Devices.ContainerId","System.Devices.Aep.ContainerId"};
  using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(7));
  var found=await DeviceInformation.FindAllAsync(selector,properties).AsTask(timeout.Token);
  var result=new List<DeviceEntry>();
  foreach(var device in found){var props=device.Properties;bool? connected=props.TryGetValue("System.Devices.Aep.IsConnected",out var value)&&value is bool b?b:null;
   string? address=props.TryGetValue("System.Devices.Aep.DeviceAddress",out var a)?a?.ToString():null;
   string? container=props.TryGetValue("System.Devices.ContainerId",out var c)?c?.ToString():null;
   if(!Guid.TryParse(container,out var guid)||guid==Guid.Empty)container=props.TryGetValue("System.Devices.Aep.ContainerId",out c)?c?.ToString():null;
   result.Add(new(device.Id,device.Name,connected,DeviceCatalog.KindFromName(device.Name),container,DeviceCatalog.NormalizeAddress(address)));
  }return result;
 }
}
