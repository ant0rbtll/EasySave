namespace EasySave.GUI.Models;

public class LogYearPickerItemModel
{
    public int Year { get; set; }
    public bool IsSelected { get; set; }

    public override string ToString() => Year.ToString();
}
