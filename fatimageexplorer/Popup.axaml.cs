using Avalonia.Controls;
using Avalonia.Interactivity;

namespace fatimageexplorer;

public partial class Popup : Window
{
    TextBlock messageText;
    TextBlock[] padding;
    Button OK_Button;
    public Popup(string message)
    {
        messageText = new TextBlock() {Text = message, Width = 400, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, TextAlignment = Avalonia.Media.TextAlignment.Center};
        padding = [new TextBlock(), new TextBlock(), new TextBlock()];
        OK_Button = new Button() {Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center};
        OK_Button.Click += OK_Click;

        Content = new StackPanel {Name = "PopupPanel", Width = 400, Height = 100,
            Children =
            {
                padding[0], messageText, padding[1], OK_Button, padding[2]
            }
        };

        Width = 400;
        Height = 100;
        
    }

    public void OK_Click(object sender, RoutedEventArgs routedEventArgs)
    {
        Close();
    }
}