#if WINDOWS
using Encryptor.Platforms.Windows;
#endif

namespace Encryptor;

public class CursorBehavior
{
    public static readonly BindableProperty CursorProperty = BindableProperty.CreateAttached(
        "Cursor", 
        typeof(CursorIcon), 
        typeof(CursorBehavior), 
        CursorIcon.Arrow, 
        propertyChanged: CursorChanged);

    private static void CursorChanged(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (bindable is VisualElement visualElement)
        {
#if WINDOWS
            // If handler is already available, set cursor immediately
            if (visualElement.Handler?.MauiContext != null)
            {
                visualElement.SetCustomCursor((CursorIcon)newvalue, visualElement.Handler.MauiContext);
            }
            else
            {
                // Otherwise, wait for handler to be created
                visualElement.HandlerChanged += OnHandlerChanged;
                
                void OnHandlerChanged(object? sender, EventArgs e)
                {
                    if (sender is VisualElement element && element.Handler?.MauiContext != null)
                    {
                        element.SetCustomCursor((CursorIcon)newvalue, element.Handler.MauiContext);
                        element.HandlerChanged -= OnHandlerChanged;
                    }
                }
            }
#endif
        }
    }

    public static CursorIcon GetCursor(BindableObject view) => (CursorIcon)view.GetValue(CursorProperty);

    public static void SetCursor(BindableObject view, CursorIcon value) => view.SetValue(CursorProperty, value);
}

public enum CursorIcon
{
    Arrow,
    Hand
}
