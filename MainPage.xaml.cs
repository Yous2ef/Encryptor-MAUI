using Encryptor.ViewModels;

namespace Encryptor;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    /// <summary>
    /// Show action sheet when FAB is clicked
    /// </summary>
    private async void OnAddButtonClicked(object sender, EventArgs e)
    {
        // Toggle popup visibility
        AddOptionsPopup.IsVisible = !AddOptionsPopup.IsVisible;
        PopupOverlay.IsVisible = AddOptionsPopup.IsVisible;
        
        // Rotate FAB icon
        if (AddOptionsPopup.IsVisible)
        {
            await FabButton.RotateToAsync(45, 200, Easing.CubicOut);
        }
        else
        {
            await FabButton.RotateToAsync(0, 200, Easing.CubicOut);
        }
    }

    /// <summary>
    /// Handle Add Files option click
    /// </summary>
    private async void OnAddFilesClicked(object sender, EventArgs e)
    {
        // Hide popup
        AddOptionsPopup.IsVisible = false;
        PopupOverlay.IsVisible = false;
        await FabButton.RotateToAsync(0, 200, Easing.CubicOut);
        
        // Execute command
        await _viewModel.AddFilesCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Handle Add Folder option click
    /// </summary>
    private async void OnAddFolderClicked(object sender, EventArgs e)
    {
        // Hide popup
        AddOptionsPopup.IsVisible = false;
        PopupOverlay.IsVisible = false;
        await FabButton.RotateToAsync(0, 200, Easing.CubicOut);
        
        // Execute command
        await _viewModel.PickFolderCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Dismiss popup when clicking outside
    /// </summary>
    private async void OnDismissPopup(object sender, EventArgs e)
    {
        AddOptionsPopup.IsVisible = false;
        PopupOverlay.IsVisible = false;
        await FabButton.RotateToAsync(0, 200, Easing.CubicOut);
    }
}
