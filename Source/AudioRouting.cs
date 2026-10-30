using System;
using System.Runtime.InteropServices;
namespace Island11;
internal static class AudioRouting {
 public static void Select(string id){var obj=(IPolicyConfig)new PolicyConfig();try{for(int role=0;role<2;role++)Marshal.ThrowExceptionForHR(obj.SetDefaultEndpoint(id,role));}finally{Marshal.ReleaseComObject(obj);}}
 [ComImport,Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")] class PolicyConfig{}
 [ComImport,Guid("F8679F50-850A-41CF-9C72-430F290290C8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IPolicyConfig {
 [PreserveSig]int GetMixFormat();[PreserveSig]int GetDeviceFormat();[PreserveSig]int ResetDeviceFormat();[PreserveSig]int SetDeviceFormat();[PreserveSig]int GetProcessingPeriod();[PreserveSig]int SetProcessingPeriod();[PreserveSig]int GetShareMode();[PreserveSig]int SetShareMode();[PreserveSig]int GetPropertyValue();[PreserveSig]int SetPropertyValue();
 [PreserveSig]int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)]string id,int role);[PreserveSig]int SetEndpointVisibility(); }
}
