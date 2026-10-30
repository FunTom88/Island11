using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
namespace Island11;

internal static class BluetoothAudio {
 static readonly PropertyKey ContainerKey = new(new Guid("8C7ED206-3F8A-4827-B3AB-AE9E1FAEFC6C"),2);
 sealed record Endpoint(string Id,string Group,string Name,string Filter,bool Connected,bool Render,string? Container,string? Address);
 static List<Endpoint> Endpoints() {
  var result=new List<Endpoint>();using var en=new MMDeviceEnumerator();
  foreach(var flow in new[]{DataFlow.Render,DataFlow.Capture})
   foreach(var d in en.EnumerateAudioEndPoints(flow,DeviceState.Active|DeviceState.Unplugged)) {
    using(d){try {
     var path=d.DeviceTopology.GetConnector(0).ConnectedToDeviceId;
     if(!path.Contains("BTH",StringComparison.OrdinalIgnoreCase))continue;
     string? container=null;
     try{container=d.Properties.Contains(ContainerKey)?d.Properties[ContainerKey].Value?.ToString():null;}catch(Exception e){Log.Write(new Exception("ContainerId: "+d.ID,e));}
     var group=Guid.TryParse(container,out var guid)&&guid!=Guid.Empty?guid.ToString():d.ID;
     var match=Regex.Match(path,@"(?i)(?:dev_|&)([0-9a-f]{12})(?:_|#|$)");
     result.Add(new(d.ID,group,d.FriendlyName,path,d.State==DeviceState.Active,flow==DataFlow.Render,container,match.Success?match.Groups[1].Value.ToUpperInvariant():null));
    }catch(Exception e){Log.Write(e);}}
   }return result;
 }
 public static List<AudioDevice> List()=>Endpoints().GroupBy(d=>d.Group).Where(g=>g.Any(d=>d.Render))
  .Select(g=>new AudioDevice(g.Key,g.First(d=>d.Render).Name,g.Any(d=>d.Connected),g.First().Container,g.First().Address)).ToList();
 public static void Request(string group, bool connect) {
  // Match physical containers, never names; disconnect both render and headset microphone profiles.
  var targets=Endpoints().Where(d=>d.Group==group).ToList();
  if(targets.Count==0)throw new InvalidOperationException("Bluetooth-аудиоустройство недоступно.");
  var failures=new List<Exception>();int succeeded=0;
  foreach(var target in targets.GroupBy(d=>d.Filter).Select(g=>g.First())) {
   try {RequestFilter(target.Filter,connect);succeeded++;}catch(Exception e){failures.Add(e);Log.Write(e);}
  }
  if(succeeded==0)throw new AggregateException("Аудиодрайвер отклонил запрос.",failures);
 }
 static void RequestFilter(string filter,bool connect){
  var enumerator=(INativeEnumerator)new NativeEnumerator();object? activated=null;INativeDevice? native=null;
  try {
   Marshal.ThrowExceptionForHR(enumerator.GetDevice(filter,out native));
   var iid=typeof(IKsControl).GUID;Marshal.ThrowExceptionForHR(native.Activate(ref iid,23,IntPtr.Zero,out activated));
   var p=new KsProperty{Set=new Guid("7FA06C40-B8F6-4C7E-8556-E8C33A12E54D"),Id=connect?0u:1u,Flags=1};
   Marshal.ThrowExceptionForHR(((IKsControl)activated).KsProperty(ref p,24,IntPtr.Zero,0,out _));
  }finally{if(activated!=null)Marshal.ReleaseComObject(activated);if(native!=null)Marshal.ReleaseComObject(native);Marshal.ReleaseComObject(enumerator);}
 }
 [StructLayout(LayoutKind.Sequential)] struct KsProperty {public Guid Set; public uint Id; public uint Flags;}
 [ComImport,Guid("6994AD04-93EF-11D0-A3CC-00A0C9223196"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IKsControl {
  [PreserveSig] int KsProperty(ref KsProperty property,uint propertyLength,IntPtr data,uint dataLength,out uint returned);
  [PreserveSig] int KsMethod(IntPtr method,uint length,IntPtr data,uint dataLength,out uint returned);
  [PreserveSig] int KsEvent(IntPtr ev,uint length,IntPtr data,uint dataLength,out uint returned);
 }
 [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class NativeEnumerator {}
 [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface INativeEnumerator {
  [PreserveSig] int EnumAudioEndpoints(int flow,uint mask,out IntPtr devices);
  [PreserveSig] int GetDefaultAudioEndpoint(int flow,int role,out IntPtr device);
  [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id,out INativeDevice device);
  [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr callback);
  [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr callback);
 }
 [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface INativeDevice {
  [PreserveSig] int Activate(ref Guid iid,uint context,IntPtr parameters,[MarshalAs(UnmanagedType.IUnknown)] out object obj);
  [PreserveSig] int OpenPropertyStore(uint access,out IntPtr properties);
  [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
  [PreserveSig] int GetState(out uint state);
 }
}
