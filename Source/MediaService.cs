using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Control;
namespace Island11;
internal sealed class MediaService {
 GlobalSystemMediaTransportControlsSessionManager? manager;
 public GlobalSystemMediaTransportControlsSession? Current {get;private set;}
 public async Task Initialize() { manager=await GlobalSystemMediaTransportControlsSessionManager.RequestAsync(); }
 public GlobalSystemMediaTransportControlsSession[] Sessions => manager?.GetSessions().ToArray() ?? Array.Empty<GlobalSystemMediaTransportControlsSession>();
 public void Select(string source) {
  var sessions=Sessions;
  Current=!string.IsNullOrEmpty(source) ? sessions.FirstOrDefault(s=>s.SourceAppUserModelId==source) : sessions.FirstOrDefault(s=>s.SourceAppUserModelId.Contains("spotify",StringComparison.OrdinalIgnoreCase));
 }
 public async Task<bool> Command(string command) { var s=Current; if(s==null) return false; return command switch {"previous"=>await s.TrySkipPreviousAsync(),"next"=>await s.TrySkipNextAsync(),_=>await s.TryTogglePlayPauseAsync()}; }
}
