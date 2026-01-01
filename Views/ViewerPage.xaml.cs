using Encryptor.ViewModels;

namespace Encryptor.Views;

public partial class ViewerPage : ContentPage
{
    private readonly ViewerViewModel _viewModel;

    public ViewerPage(ViewerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            System.Diagnostics.Debug.WriteLine("ViewerPage: OnAppearing - calling InitializeAsync");
            await _viewModel.InitializeAsync();
            System.Diagnostics.Debug.WriteLine("ViewerPage: InitializeAsync completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ViewerPage: OnAppearing error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ViewerPage: Stack trace: {ex.StackTrace}");
            
            await DisplayAlert("Error", $"Failed to load file: {ex.Message}", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Stop media playback when leaving
        try
        {
            if (MediaPlayer is not null)
            {
                MediaPlayer.Stop();
                MediaPlayer.Source = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping media player: {ex.Message}");
        }
    }

    private void OnMediaFailed(object sender, CommunityToolkit.Maui.Core.Primitives.MediaFailedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Playback failed: {e.ErrorMessage}");

    }
}
