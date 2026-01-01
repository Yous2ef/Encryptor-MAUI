using System.Globalization;
using Encryptor.Models;

namespace Encryptor.Converters;

/// <summary>
/// Converts FileCategory or file extension to an appropriate icon glyph/image.
/// Uses Unicode symbols for cross-platform compatibility.
/// </summary>
public class FileIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        FileCategory category;

        if (value is FileModel fileModel)
        {
            category = fileModel.Category;
        }
        else if (value is FileCategory cat)
        {
            category = cat;
        }
        else if (value is string extension)
        {
            category = GetCategoryFromExtension(extension);
        }
        else
        {
            category = FileCategory.Unknown;
        }

        return category switch
        {
            FileCategory.Image => "🖼️",
            FileCategory.Video => "🎬",
            FileCategory.Audio => "🎵",
            FileCategory.Text => "📄",
            FileCategory.Document => "📑",
            FileCategory.Archive => "📦",
            FileCategory.Encrypted => "🔒",
            FileCategory.Unknown => "📎",
            _ => "📎"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();

    private static FileCategory GetCategoryFromExtension(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" => FileCategory.Image,
        ".mp4" or ".mkv" or ".avi" or ".mov" or ".webm" => FileCategory.Video,
        ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" => FileCategory.Audio,
        ".txt" or ".json" or ".xml" or ".html" or ".htm" or ".css" or ".js" or ".cs" => FileCategory.Text,
        ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" => FileCategory.Document,
        ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => FileCategory.Archive,
        ".enc" => FileCategory.Encrypted,
        _ => FileCategory.Unknown
    };
}

/// <summary>
/// Converts IsEncrypted boolean to a lock/unlock status text.
/// </summary>
public class EncryptionStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEncrypted)
        {
            return isEncrypted ? "🔒 Encrypted" : "🔓 Unlocked";
        }
        return "❓ Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts IsEncrypted boolean to a badge color.
/// </summary>
public class EncryptionStatusColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEncrypted)
        {
            return isEncrypted 
                ? Color.FromArgb("#10b981")  // Green for encrypted
                : Color.FromArgb("#f59e0b"); // Orange for unlocked
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts IsSelected boolean to a selection indicator color.
/// </summary>
public class SelectionColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected)
        {
            return isSelected ? Color.FromArgb("#1077b7") : Colors.Transparent;
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return false;
    }
}

/// <summary>
/// Converts IsEncrypted to visibility - shows Open button only when unlocked.
/// </summary>
public class UnlockedToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEncrypted)
        {
            return !isEncrypted; // Show when NOT encrypted (unlocked)
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts theme preference to icon.
/// </summary>
public class ThemeIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDarkMode)
        {
            return isDarkMode ? "☀️" : "🌙";
        }
        return "🌙";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts password visibility toggle to eye icon.
/// </summary>
public class PasswordVisibilityIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isPasswordHidden)
        {
            return isPasswordHidden ? "👁️" : "👁️‍🗨️";
        }
        return "👁️";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Checks if value is null (returns false if null, true if not null).
/// </summary>
public class IsNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
