using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text.Json;
using NAudio.CoreAudioApi;
using Windows.Devices.Enumeration;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Forms=System.Windows.Forms;
namespace Island11;
public partial class MainWindow : Window {
 string section="music"; bool clipboardOn; string lastClip=""; readonly List<string> clips=new();
 readonly Settings settings=Settings.Load(); readonly MediaService media=new();
 readonly DeviceService deviceService=new();
 readonly TrackTimeline timeline=new();
 readonly DispatcherTimer progressTimer=new(){Interval=TimeSpan.FromMilliseconds(50)};
 IReadOnlyList<DeviceEntry> lastDevices=Array.Empty<DeviceEntry>();
 readonly DispatcherTimer tick=new(){Interval=TimeSpan.FromSeconds(1)};
 readonly DispatcherTimer animation=new(){Interval=TimeSpan.FromMilliseconds(16)};
 readonly DispatcherTimer collapse=new(){Interval=TimeSpan.FromMilliseconds(550)};
 readonly Forms.NotifyIcon tray=new();
 bool expanded, dragging, readingMedia, readingDevices, updatingVolume, volumeUserAction, btBusy, menuOpen, closing;
 int ticks; double target=0, progress=0; DateTime animationStart;
 double fromProgress; Point dragStart, anchorStart; IntPtr hwnd;
 string artworkKey=""; string deviceKey="";
 public MainWindow() {
  InitializeComponent();
  Loaded+=LoadedWindow;
  progressTimer.Tick+=(_,_)=>PaintProgress();
  IsVisibleChanged+=(_,_)=>{if(IsVisible)progressTimer.Start();else progressTimer.Stop();};
  tick.Tick+=async(_,_)=> { Clock.Text=DateTime.Now.ToString("HH:mm"); if(IsVisible&&!dragging)Position(); ++ticks; ReadClipboard(); await RefreshMedia(); if(expanded){RefreshVolume();if(ticks%8==0)await RefreshDevices();} };
  collapse.Tick+=(_,_)=>{collapse.Stop();if(!Shell.IsMouseOver&&!dragging&&!menuOpen&&!btBusy)SetExpanded(false);};
  animation.Tick+=(_,_)=>Animate();
  Closed+=(_,_)=>{closing=true;progressTimer.Stop();tick.Stop();collapse.Stop();animation.Stop();tray.Visible=false;tray.Dispose();settings.Save();};
  DpiChanged+=(_,_)=>Dispatcher.BeginInvoke(new Action(Position));
 }
 async void LoadedWindow(object sender,RoutedEventArgs e) {
  hwnd=new WindowInteropHelper(this).Handle; LoadExtras();
  if(!double.IsFinite(settings.CenterX)||!double.IsFinite(settings.BottomY)) ResetPosition(); else Position();
  SetupTray(); Clock.Text=DateTime.Now.ToString("HH:mm"); tick.Start();progressTimer.Start();
  try {await media.Initialize();await RefreshMedia();} catch(Exception ex){Report("Медиасессии Windows недоступны",ex);}
 }
 void SetupTray() {
  tray.Icon=System.Drawing.SystemIcons.Application; tray.Text="Island11 0.4.2";
  var menu=new Forms.ContextMenuStrip();
  menu.Items.Add("Показать / скрыть",null,(_,_)=>Dispatcher.Invoke(()=>{if(IsVisible)Hide();else{Show();Position();}}));
  menu.Items.Add("Вернуть на панель задач",null,(_,_)=>Dispatcher.Invoke(()=>{Show();ResetPosition();}));
  var startup=new Forms.ToolStripMenuItem("Автозапуск Windows"){Checked=Settings.AutoStart,CheckOnClick=true};
  startup.Click+=(_,_)=>{try{Settings.SetAutoStart(startup.Checked);}catch(Exception ex){startup.Checked=Settings.AutoStart;Report("Не удалось изменить автозапуск",ex);}};
  menu.Opening+=(_,_)=>startup.Checked=Settings.AutoStart;
  menu.Items.Add(startup); menu.Items.Add("Выход",null,(_,_)=>Dispatcher.Invoke(Close));
  tray.ContextMenuStrip=menu; tray.DoubleClick+=(_,_)=>Dispatcher.Invoke(()=>{Show();Position();});tray.Visible=true;
 }
 void ResetPosition() {
  var screen=Forms.Screen.PrimaryScreen!;var r=screen.Bounds;var work=screen.WorkingArea;
  // Default left segment avoids Windows 11's centred application buttons.
  settings.CenterX=r.Left+Math.Min(230,r.Width/3);settings.BottomY=work.Bottom<r.Bottom?r.Bottom-6:work.Bottom-8;
  Position();settings.Save();
 }
 void Position() {
  if(hwnd==IntPtr.Zero)return;
  var screen=Forms.Screen.FromPoint(new System.Drawing.Point((int)settings.CenterX,(int)settings.BottomY));var r=screen.Bounds;
  var dpi=VisualTreeHelper.GetDpi(this);var w=(int)Math.Ceiling(Width*dpi.DpiScaleX);var h=(int)Math.Ceiling(Height*dpi.DpiScaleY);
  var x=(int)Math.Clamp(settings.CenterX-w/2,r.Left,Math.Max(r.Left,r.Right-w));
  var y=(int)Math.Clamp(extras.Top?settings.BottomY-38*dpi.DpiScaleY:settings.BottomY-h,r.Top,Math.Max(r.Top,r.Bottom-h));
  SetWindowPos(hwnd,new IntPtr(-1),x,y,w,h,0x10);
 }
 void SetExpanded(bool value) {
  if(expanded==value)return; expanded=value;target=value?1:0;fromProgress=progress;animationStart=DateTime.UtcNow;
  if(value){ExpandedContainer.Visibility=Visibility.Visible;_ = RefreshDevices();RefreshVolume();}
  animation.Start();
 }
 void Animate() {
  var t=Math.Clamp((DateTime.UtcNow-animationStart).TotalMilliseconds/280,0,1);var eased=1-Math.Pow(1-t,3);
  progress=fromProgress+(target-fromProgress)*eased;Width=(244+196*progress)*extras.Scale;Height=(38+562*progress)*extras.Scale;
  Shell.CornerRadius=new CornerRadius(19+7*progress);
  EdgeProgress.Opacity=1-progress;ExpandedContainer.Opacity=progress;ExpandedContainer.IsHitTestVisible=progress>.98;Position();
  if(t>=1){animation.Stop();if(!expanded)ExpandedContainer.Visibility=Visibility.Collapsed;}
 }
 void EnterIsland(object sender,MouseEventArgs e){collapse.Stop();if(!dragging)SetExpanded(true);}
 void LeaveIsland(object sender,MouseEventArgs e){if(!dragging){collapse.Stop();collapse.Start();}}
 void StartDrag(object sender,MouseButtonEventArgs e){collapse.Stop();dragging=true;var p=Forms.Cursor.Position;dragStart=new(p.X,p.Y);anchorStart=new(settings.CenterX,settings.BottomY);((UIElement)sender).CaptureMouse();e.Handled=true;}
 void ContinueDrag(object sender,MouseEventArgs e){if(!dragging)return;var p=Forms.Cursor.Position;settings.CenterX=anchorStart.X+p.X-dragStart.X;settings.BottomY=anchorStart.Y+p.Y-dragStart.Y;Position();}
 void EndDrag(object sender,MouseButtonEventArgs e){if(!dragging)return;dragging=false;((UIElement)sender).ReleaseMouseCapture();var screen=Forms.Screen.FromPoint(Forms.Cursor.Position);settings.CenterX=Math.Clamp(settings.CenterX,screen.Bounds.Left+122,screen.Bounds.Right-122);settings.BottomY=Math.Clamp(settings.BottomY,screen.Bounds.Top+38,screen.Bounds.Bottom);Position();settings.Save();if(!Shell.IsMouseOver)collapse.Start();}
 void DragCaptureLost(object sender,MouseEventArgs e){if(dragging){dragging=false;settings.Save();if(!Shell.IsMouseOver)collapse.Start();}}
 void RightClick(object sender,MouseButtonEventArgs e)=>ShowMenu();
 void OpenMenu(object sender,RoutedEventArgs e)=>ShowMenu();
 void DisplayMenu(ContextMenu menu){menu.PlacementTarget=Shell;menu.Opened+=(_,_)=>{menuOpen=true;collapse.Stop();};menu.Closed+=(_,_)=>{menuOpen=false;collapse.Start();};menu.IsOpen=true;}
 void ShowMenu(){
  var m=new ContextMenu();
  AddMenu(m,"Вернуть на панель задач",ResetPosition);
  var auto=new MenuItem{Header="Автозапуск Windows",IsCheckable=true,IsChecked=Settings.AutoStart};auto.Click+=(_,_)=>{try{Settings.SetAutoStart(auto.IsChecked);}catch(Exception ex){Report("Ошибка автозапуска",ex);}};m.Items.Add(auto);
  AddMenu(m,"Открыть Spotify",()=>Launch("https://open.spotify.com"));
  AddMenu(m,"Параметры Bluetooth",()=>Launch("ms-settings:bluetooth"));
  AddMenu(m,"Параметры звука",()=>Launch("ms-settings:sound"));
  AddMenu(m,"О программе: Island11 0.4.2",()=>Status.Text="Island11 0.4.2 • прогресс трека и Bluetooth");
  AddMenu(m,"Открыть журнал",()=>{Directory.CreateDirectory(Log.Folder);Launch(Log.Folder);});
  AddMenu(m,"Скрыть в трей",Hide);AddMenu(m,"Выход",Close);DisplayMenu(m);
 }
 static void AddMenu(ContextMenu m,string label,Action action){var item=new MenuItem{Header=label};item.Click+=(_,_)=>action();m.Items.Add(item);}
 void SelectSource(object sender,RoutedEventArgs e){var m=new ContextMenu();AddMenu(m,"Авто: только приложение Spotify",()=>{settings.MediaSource="";settings.Save();_ = RefreshMedia();});foreach(var s in media.Sessions){var id=s.SourceAppUserModelId;AddMenu(m,id,()=>{settings.MediaSource=id;settings.Save();_ = RefreshMedia();});}DisplayMenu(m);}
 async Task RefreshMedia(){
  if(readingMedia||closing)return;readingMedia=true;
  try{
   media.Select(settings.MediaSource);var s=media.Current;
   if(s==null){Track.Text="Открой Spotify";Artist.Text="В браузере: включи трек и выбери источник";CompactTitle.Text="Island11";Cover.Source=null;artworkKey="";Play.IsEnabled=Next.IsEnabled=Previous.IsEnabled=false;SourceLabel.Text="Выбрать источник ▾";SetPlaying(false);timeline.Clear();PaintProgress();return;}
   var playback=s.GetPlaybackInfo();SetPlaying(playback.PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing);
   var p=await s.TryGetMediaPropertiesAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(4));
   Track.Text=string.IsNullOrWhiteSpace(p.Title)?"Без названия":p.Title;Artist.Text=p.Artist;CompactTitle.Text=Track.Text;
   SourceLabel.Text=FriendlySource(s.SourceAppUserModelId)+" ▾";SourceButton.ToolTip=s.SourceAppUserModelId;
   
   Play.IsEnabled=playback.Controls.IsPlayPauseToggleEnabled;Next.IsEnabled=playback.Controls.IsNextEnabled;Previous.IsEnabled=playback.Controls.IsPreviousEnabled;
   var key=s.SourceAppUserModelId+"|"+p.Title+"|"+p.Artist+"|"+p.AlbumTitle;
   try{var t=s.GetTimelineProperties();timeline.Update(new TimelineSample(key,t.StartTime.TotalSeconds,t.EndTime.TotalSeconds,t.Position.TotalSeconds,t.LastUpdatedTime),playback.PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,playback.PlaybackRate??1,DateTimeOffset.UtcNow);}catch(Exception ex){Log.Write(ex);timeline.Clear();}
   PaintProgress();
   if(key!=artworkKey){artworkKey=key;Cover.Source=null;if(p.Thumbnail!=null){using var stream=await p.Thumbnail.OpenReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(4));if(stream.Size<8*1024*1024){using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var data=new byte[(int)stream.Size];reader.ReadBytes(data);using var mem=new MemoryStream(data);var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=mem;bitmap.DecodePixelWidth=128;bitmap.EndInit();bitmap.Freeze();Cover.Source=bitmap;}}}
  }catch(Exception ex){Log.Write(ex);SetPlaying(false);timeline.Clear();PaintProgress();Artist.Text="Не удалось прочитать медиасессию";}finally{readingMedia=false;}
 }
 void PaintProgress(){if(closing)return;var now=DateTimeOffset.UtcNow;double fraction=timeline.Fraction(now);EdgeProgress.Fraction=fraction;InsideProgress.Fraction=fraction;ElapsedLabel.Text=timeline.Available?TrackTimeline.Format(timeline.Position(now)):"—:—";DurationLabel.Text=timeline.Available?TrackTimeline.Format(timeline.Duration):"—:—";}
 void SetPlaying(bool playing){LargeEq.Playing=playing;CompactEq.Playing=playing;PlayGlyph.Kind=playing?"pause":"play";}
 static string FriendlySource(string source){if(source.Contains("spotify",StringComparison.OrdinalIgnoreCase))return "Spotify";if(source.Contains("chrome",StringComparison.OrdinalIgnoreCase))return "Google Chrome";if(source.Contains("msedge",StringComparison.OrdinalIgnoreCase))return "Microsoft Edge";if(source.Contains("firefox",StringComparison.OrdinalIgnoreCase))return "Firefox";return source;}
 async Task MediaCommand(string command){try{if(!await media.Command(command).WaitAsync(TimeSpan.FromSeconds(4)))Status.Text="Источник не поддерживает эту команду";await RefreshMedia();}catch(Exception ex){Report("Не удалось управлять плеером",ex);}}
 async void PreviousClick(object s,RoutedEventArgs e)=>await MediaCommand("previous");
 async void NextClick(object s,RoutedEventArgs e)=>await MediaCommand("next");
 async void PlayClick(object s,RoutedEventArgs e)=>await MediaCommand("play");
 void RefreshVolume(){try{using var en=new MMDeviceEnumerator();using var dev=en.GetDefaultAudioEndpoint(DataFlow.Render,Role.Multimedia);updatingVolume=true;if(!Volume.IsMouseCaptureWithin)Volume.Value=dev.AudioEndpointVolume.MasterVolumeLevelScalar*100;VolumeLabel.Text=$"{Volume.Value:0}%";Volume.IsEnabled=true;}catch{Volume.IsEnabled=false;VolumeLabel.Text="—";}finally{updatingVolume=false;}}
 void VolumeMouseDown(object sender,MouseButtonEventArgs e){volumeUserAction=true;}
 void VolumeMouseUp(object sender,MouseButtonEventArgs e){Dispatcher.BeginInvoke(new Action(()=>volumeUserAction=false),DispatcherPriority.Input);}
 void VolumeCaptureLost(object sender,MouseEventArgs e){Dispatcher.BeginInvoke(new Action(()=>volumeUserAction=false),DispatcherPriority.Input);}
 void VolumeKeyDown(object sender,KeyEventArgs e){if(e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown){volumeUserAction=true;Dispatcher.BeginInvoke(new Action(()=>volumeUserAction=false),DispatcherPriority.Input);}}
 void VolumeChanged(object sender,RoutedPropertyChangedEventArgs<double> e){
  if(!VolumeWritePolicy.ShouldWrite(IsLoaded,updatingVolume,volumeUserAction,e.OldValue,e.NewValue))return;
  try{using var en=new MMDeviceEnumerator();using var dev=en.GetDefaultAudioEndpoint(DataFlow.Render,Role.Multimedia);dev.AudioEndpointVolume.MasterVolumeLevelScalar=(float)(e.NewValue/100);VolumeLabel.Text=$"{e.NewValue:0}%";}catch(Exception ex){Report("Нет доступного аудиовыхода",ex);}
 }
 async void RefreshBluetoothClick(object sender,RoutedEventArgs e){deviceKey="";await RefreshDevices();}
 async Task RefreshDevices(){
  if(readingDevices||btBusy||closing)return;readingDevices=true;RefreshBluetooth.IsEnabled=false;
  if(Devices.Children.Count==0)Devices.Children.Add(new TextBlock{Text="Загрузка устройств…",Foreground=Brushes.Gray,Margin=new Thickness(4,14,0,0)});
  try{
   var snapshot=await deviceService.Read();if(closing)return;
   try{Directory.CreateDirectory(Log.Folder);File.WriteAllText(Path.Combine(Log.Folder,"devices.json"),JsonSerializer.Serialize(snapshot,new JsonSerializerOptions{WriteIndented=true}));}catch(Exception ex){Log.Write(ex);}
   IReadOnlyList<DeviceEntry> entries=snapshot.Devices;
   if(entries.Count==0&&snapshot.Errors.Count>0&&lastDevices.Count>0)entries=lastDevices.Select(d=>d with {Connected=null,Audio=null}).ToArray();
   else lastDevices=entries;
   DeviceCount.Text=entries.Count==0?"":entries.Count+" устр.";
   DeviceCount.ToolTip=snapshot.Errors.Count>0?string.Join("\n",snapshot.Errors):"Список обновлён";
   var key=string.Join("|",entries.Select(d=>d.Key+d.Name+d.Connected+d.Kind+(d.Audio?.Id??"")));
   if(deviceKey==key&&Devices.Children.Count>0&&entries.Count>0)return;deviceKey=key;Devices.Children.Clear();
   foreach(var device in entries)Devices.Children.Add(DeviceCard(device));
   if(entries.Count==0){Devices.Children.Add(new TextBlock{Text=snapshot.Errors.Count>0?"Не удалось прочитать устройства. Нажми ↻ для повтора. Подробности — в журнале.":"Сопряжённых Bluetooth-устройств пока нет.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Gray,Margin=new Thickness(4,12,4,8)});}
   if(snapshot.Errors.Count>0)Status.Text=entries.Count>0?"Часть источников недоступна • найденные устройства показаны":"Ошибка чтения Bluetooth • меню → Открыть журнал";
   else if(Status.Text.StartsWith("Часть источников")||Status.Text.StartsWith("Ошибка чтения"))Status.Text="Перетаскивай за нижнюю полоску • ПКМ — меню";
  }catch(Exception ex){Report("Не удалось обновить Bluetooth • подробности в журнале",ex);}finally{readingDevices=false;RefreshBluetooth.IsEnabled=true;}
 }
 Border DeviceCard(DeviceEntry device){
  var card=new Border{CornerRadius=new CornerRadius(15),Background=(Brush)FindResource("Surface"),BorderBrush=new SolidColorBrush(Color.FromRgb(41,45,52)),BorderThickness=new Thickness(1),Padding=new Thickness(12,9,10,9),Margin=new Thickness(0,0,0,7)};
  var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(30)});row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
  var icon=new Glyph{Kind=device.Kind,Width=23,Height=23,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Left,Foreground=new SolidColorBrush(Color.FromRgb(225,229,234))};row.Children.Add(icon);
  var text=new StackPanel{Margin=new Thickness(5,0,9,0),VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(text,1);
  text.Children.Add(new TextBlock{Text=device.Name,FontSize=12,TextTrimming=TextTrimming.CharacterEllipsis,ToolTip=device.Name});
  string state=device.Connected switch{true=>device.Audio!=null?"Аудио подключено":"Подключено",false=>"Отключено",_=>"Статус недоступен"};
  var brush=device.Connected==true?(Brush)FindResource("Green"):(Brush)FindResource("Muted");
  text.Children.Add(new TextBlock{Text="●  "+state,FontSize=10,Foreground=brush,Margin=new Thickness(0,4,0,0)});row.Children.Add(text);
  if(device.Audio is AudioDevice audio){var button=new Button{Content=audio.Connected?"Отключить":"Подключить",FontSize=10,MinWidth=91,Height=29,Padding=new Thickness(10,5,10,5),VerticalAlignment=VerticalAlignment.Center,IsEnabled=!btBusy};Grid.SetColumn(button,2);button.Click+=async(_,_)=>await ToggleAudio(audio,button);row.Children.Add(button);}
  else if(device.Kind=="headphones")card.ToolTip="Устройство найдено. Управление появится, когда Windows предоставит его Bluetooth-аудиопрофиль.";
  card.Child=row;return card;
 }
 void SetAudioButtonsEnabled(bool enabled){foreach(var card in Devices.Children.OfType<Border>())if(card.Child is Grid grid)foreach(var button in grid.Children.OfType<Button>())button.IsEnabled=enabled;}
 async Task ToggleAudio(AudioDevice device,Button button){
  if(btBusy)return;btBusy=true;SetAudioButtonsEnabled(false);collapse.Stop();var connect=!device.Connected;Status.Text=connect?"Подключаю наушники…":"Отключаю наушники…";
  try{
   var request=Task.Run(()=>BluetoothAudio.Request(device.Id,connect));
   try{await request.WaitAsync(TimeSpan.FromSeconds(8));}catch(TimeoutException){Status.Text="Аудиодрайвер отвечает долго. Ожидаю завершения…";await request;}
   bool verified=false;for(int i=0;i<10;i++){await Task.Delay(700);var devices=await Task.Run(BluetoothAudio.List).WaitAsync(TimeSpan.FromSeconds(3));var found=devices.FirstOrDefault(a=>a.Id==device.Id);if(found!=null&&found.Connected==connect){verified=true;break;}}
   Status.Text=verified?(connect?"Аудио подключено":"Аудио отключено"):"Драйвер не подтвердил результат • проверь наушники";
  }catch(Exception ex){Report("Не удалось переключить аудио • подробности в журнале",ex);}finally{btBusy=false;SetAudioButtonsEnabled(true);deviceKey="";await RefreshDevices();if(!Shell.IsMouseOver)collapse.Start();}
 }
 void BluetoothSettings(object s,RoutedEventArgs e)=>Launch("ms-settings:bluetooth");
 void Launch(string target){try{Process.Start(new ProcessStartInfo(target){UseShellExecute=true});}catch(Exception ex){Report("Не удалось открыть",ex);}}
 void Report(string message,Exception e){Log.Write(e);if(!closing)Status.Text=message;}
 [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int cx,int cy,uint flags);
}
