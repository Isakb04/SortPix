using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace SortPix;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);

        const int newWidth = 1311;
        const int newHeight = 786;
        
        window.X = (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density - newWidth) / 2;
        window.Y = (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density - newHeight) / 2;
        
        window.Width = newWidth;
        window.Height = newHeight;

        window.Title = "SortPix File Manager";

        return window;
    }
}