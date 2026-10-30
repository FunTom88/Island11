using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
namespace Island11;
public partial class MainWindow {
 DateTime calendarMonth=new(DateTime.Today.Year,DateTime.Today.Month,1);
 DateTime calendarSelected=DateTime.Today;
 static readonly CultureInfo CalendarCulture=CultureInfo.GetCultureInfo("ru-RU");
 void BuildDarkCalendar(){
  ExtraTitle.Text="Дата и время";
  ExtraItems.Children.Add(new TextBlock{Text=calendarSelected.ToString("dddd, d MMMM yyyy",CalendarCulture),FontSize=16,Margin=new Thickness(0,0,0,14),Foreground=Brushes.White,TextWrapping=TextWrapping.Wrap});
  var card=new Border{Background=new SolidColorBrush(Color.FromRgb(24,27,32)),BorderBrush=new SolidColorBrush(Color.FromRgb(49,54,63)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(16),Padding=new Thickness(12),Margin=new Thickness(0,0,0,8)};
  var body=new StackPanel();var header=new Grid{Margin=new Thickness(0,0,0,10)};
  header.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(38)});header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(38)});
  Button monthButton(string label,int direction){var b=new Button{Content=label,Width=34,Height=32,Padding=new Thickness(0),ToolTip=direction<0?"Предыдущий месяц":"Следующий месяц"};b.Click+=(_,_)=>{try{calendarMonth=calendarMonth.AddMonths(direction);BuildPage();}catch(ArgumentOutOfRangeException){}};return b;}
  var prev=monthButton("‹",-1);header.Children.Add(prev);
  var title=new TextBlock{Text=CalendarCulture.TextInfo.ToTitleCase(calendarMonth.ToString("MMMM yyyy",CalendarCulture)),FontSize=14,FontWeight=FontWeights.SemiBold,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};Grid.SetColumn(title,1);header.Children.Add(title);
  var next=monthButton("›",1);Grid.SetColumn(next,2);header.Children.Add(next);body.Children.Add(header);
  var weekday=new UniformGrid{Columns=7,Margin=new Thickness(0,0,0,5)};foreach(var name in new[]{"ПН","ВТ","СР","ЧТ","ПТ","СБ","ВС"})weekday.Children.Add(new TextBlock{Text=name,FontSize=10,Foreground=new SolidColorBrush(Color.FromRgb(157,165,176)),HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,3,0,3)});body.Children.Add(weekday);
  var days=new UniformGrid{Columns=7,Rows=6};for(int i=0;i<42;i++){
   var date=CalendarLogic.Cell(calendarMonth,i);bool today=date.Date==DateTime.Today,selected=date.Date==calendarSelected.Date,inMonth=date.Month==calendarMonth.Month;
   var b=new Button{Content=new TextBlock{Text=date.Day.ToString(CalendarCulture),Foreground=new SolidColorBrush(inMonth?Color.FromRgb(242,243,245):Color.FromRgb(120,126,136))},Height=35,Margin=new Thickness(2),Padding=new Thickness(0),FontSize=12,Background=new SolidColorBrush(selected?Color.FromRgb(38,112,71):Color.FromRgb(24,27,32)),BorderBrush=new SolidColorBrush(today?Color.FromRgb(75,218,135):selected?Color.FromRgb(65,150,96):Color.FromRgb(24,27,32)),BorderThickness=new Thickness(today||selected?1:0),Foreground=new SolidColorBrush(inMonth?Color.FromRgb(242,243,245):Color.FromRgb(120,126,136)),ToolTip=date.ToString("D",CalendarCulture)};
   b.Click+=(_,_)=>{calendarSelected=date.Date;calendarMonth=new DateTime(date.Year,date.Month,1);BuildPage();};days.Children.Add(b);
  }
  body.Children.Add(days);card.Child=body;ExtraItems.Children.Add(card);
  ExtraItems.Children.Add(Btn("Сегодня",()=>{calendarSelected=DateTime.Today;calendarMonth=new DateTime(calendarSelected.Year,calendarSelected.Month,1);BuildPage();}));
 }
}
