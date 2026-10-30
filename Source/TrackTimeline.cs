using System;
namespace Island11;
internal readonly record struct TimelineSample(string Key,double Start,double End,double Position,DateTimeOffset Updated);
internal sealed class TrackTimeline {
 TimelineSample? sample;
 double anchorPosition,rate=1;
 DateTimeOffset anchorTime,lastPoll;
 bool playing;
 public double Duration{get;private set;}
 public bool Available=>sample.HasValue&&Duration>0;
 public void Clear(){sample=null;Duration=0;playing=false;anchorPosition=0;}
 public void Update(TimelineSample current,bool isPlaying,double playbackRate,DateTimeOffset now){
  if(!double.IsFinite(current.Start)||!double.IsFinite(current.End)||!double.IsFinite(current.Position)||current.End<=current.Start){Clear();return;}
  var nextRate=double.IsFinite(playbackRate)&&playbackRate>0?playbackRate:1;
  bool changed=sample!=current;
  double previous=Position(now);
  if(changed){
   Duration=current.End-current.Start;
   double age=(now-current.Updated).TotalSeconds;
   // Position is relative to LastUpdatedTime. Ignore unusable timestamps.
   if(!double.IsFinite(age)||age<0||age>Duration)age=0;
   anchorPosition=Math.Clamp(current.Position-current.Start+(isPlaying?age*nextRate:0),0,Duration);
   anchorTime=now;
  }else if(isPlaying!=playing||nextRate!=rate){
   // Some players change play/pause without refreshing timeline properties.
   // Preserve the observed position; do not add time spent paused.
   anchorPosition=previous;anchorTime=now;
  }
  sample=current;rate=nextRate;playing=isPlaying;lastPoll=now;
 }
 public double Position(DateTimeOffset now){if(!Available)return 0;var time=now;
  // Stop extrapolating if the media provider stops responding.
  if(time>lastPoll.AddSeconds(5))time=lastPoll.AddSeconds(5);
  double elapsed=playing?Math.Max(0,(time-anchorTime).TotalSeconds)*rate:0;
  return Math.Clamp(anchorPosition+elapsed,0,Duration);
 }
 public double Fraction(DateTimeOffset now)=>Available?Position(now)/Duration:0;
 internal static string Format(double seconds){if(!double.IsFinite(seconds)||seconds<0)return "—:—";var total=(long)Math.Floor(seconds);return total>=3600?$"{total/3600}:{total/60%60:00}:{total%60:00}":$"{total/60}:{total%60:00}";}
}
