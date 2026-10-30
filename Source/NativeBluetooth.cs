using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
namespace Island11;
internal static class NativeBluetooth {
 // Read remembered devices without inquiry, pairing, or changing radio state.
 public static List<DeviceEntry> List(){
  var result=new List<DeviceEntry>();var search=new Search{Size=(uint)Marshal.SizeOf<Search>(),Authenticated=1,Remembered=1,Connected=1};
  var info=new Info{Size=(uint)Marshal.SizeOf<Info>(),Name=""};var handle=BluetoothFindFirstDevice(ref search,ref info);
  if(handle==IntPtr.Zero){int error=Marshal.GetLastWin32Error();if(error!=0&&error!=259)throw new Win32Exception(error);return result;}
  try{do{string address=info.Address.ToString("X12");var kind=DeviceCatalog.KindFromName(info.Name);if(((info.Class>>8)&31)==5){if((info.Class&0x40)!=0)kind="keyboard";else if((info.Class&0x80)!=0)kind="mouse";}else if(((info.Class>>8)&31)==4)kind="headphones";
   result.Add(new("classic:"+address,string.IsNullOrWhiteSpace(info.Name)?"Bluetooth-устройство":info.Name,info.Connected!=0,kind,Address:address));
   info.Size=(uint)Marshal.SizeOf<Info>();
  }while(BluetoothFindNextDevice(handle,ref info));
  int error=Marshal.GetLastWin32Error();if(error!=0&&error!=259)throw new Win32Exception(error);
  }finally{BluetoothFindDeviceClose(handle);}return result;
 }
 [StructLayout(LayoutKind.Sequential)] internal struct Search{public uint Size;public int Authenticated,Remembered,Unknown,Connected,Inquiry;public byte Timeout;public IntPtr Radio;}
 [StructLayout(LayoutKind.Sequential)] internal struct SystemTime{public ushort Year,Month,WeekDay,Day,Hour,Minute,Second,Milliseconds;}
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] internal struct Info{public uint Size;public ulong Address;public uint Class;public int Connected,Remembered,Authenticated;public SystemTime LastSeen,LastUsed;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=248)]public string Name;}
 [DllImport("bthprops.cpl",SetLastError=true)] static extern IntPtr BluetoothFindFirstDevice(ref Search search,ref Info info);
 [DllImport("bthprops.cpl",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] static extern bool BluetoothFindNextDevice(IntPtr handle,ref Info info);
 [DllImport("bthprops.cpl",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] static extern bool BluetoothFindDeviceClose(IntPtr handle);
}
