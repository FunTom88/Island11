using System;
namespace Island11;
internal static class PlaybackVisual {
 // Decorative playback indicator; deliberately not a spectrum analyser.
 internal static double Bar(double seconds,int index)=>0.23+0.77*(0.5+0.5*Math.Sin(seconds*(3.7+index*.47)+index*1.7));
}
