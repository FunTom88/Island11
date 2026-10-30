using System;
namespace Island11;
internal static class CalendarLogic {
 internal static DateTime FirstCell(DateTime month){var first=new DateTime(month.Year,month.Month,1);var offset=((int)first.DayOfWeek+6)%7;return first.AddDays(-offset);}
 internal static DateTime Cell(DateTime month,int index){if(index<0||index>=42)throw new ArgumentOutOfRangeException(nameof(index));return FirstCell(month).AddDays(index);}
}
