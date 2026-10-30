using System;
namespace Island11;
internal static class VolumeWritePolicy {
 internal static bool ShouldWrite(bool loaded,bool syncing,bool userAction,double oldValue,double newValue)=>loaded&&!syncing&&userAction&&double.IsFinite(newValue)&&newValue>=0&&newValue<=100&&Math.Abs(newValue-oldValue)>0.00001;
}
