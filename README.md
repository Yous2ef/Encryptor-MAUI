# 🔐 Encryptor - مُشفر

[![Platform](https://img.shields.io/badge/platform-Android%20%7C%20Windows-blue.svg)](https://dotnet.microsoft.com/apps/maui)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Release](https://img.shields.io/badge/version-1.0.0-orange.svg)](https://github.com/Yous2ef/Encryptor/releases)

A modern, cross-platform file encryption application built with .NET MAUI. Encrypt and decrypt your files securely with AES-256 encryption on Android and Windows.

<div align="center">
  <img src="Resources/AppIcon/logo.svg" alt="Encryptor Logo" width="128" height="128"/>
</div>

## ✨ Features

### 🔒 Security
- **AES-256-CBC Encryption**: Military-grade encryption using 256-bit keys
- **PBKDF2 Key Derivation**: Secure key derivation with 100,000 iterations (OWASP recommended)
- **Salt & IV Generation**: Random salt and initialization vector for each file
- **Memory Protection**: Automatic memory cleanup of decrypted data

### 📁 File Management
- **Multi-file Support**: Encrypt/decrypt multiple files at once
- **Folder Encryption**: Recursively encrypt entire folders
- **Smart Processing**: Automatically skips already encrypted/decrypted files
- **In-Memory Decryption**: Preview encrypted files without saving to disk
- **Batch Operations**: Process multiple files with progress tracking

### 🎨 User Interface
- **Modern Design**: Clean, intuitive Material Design-inspired UI
- **Dark/Light Themes**: Automatic theme switching with manual override
- **Responsive Layout**: Adapts to different screen sizes and orientations
- **File Previews**: Thumbnail generation for images and videos
- **Progress Tracking**: Real-time progress indicators for operations

### 💾 Platform Support

<table>
  <tr>
    <td align="center"><strong>Platform</strong></td>
    <td align="center"><strong>Status</strong></td>
    <td align="center"><strong>Minimum Version</strong></td>
  </tr>
  <tr>
    <td align="center">🤖 <strong>Android</strong></td>
    <td align="center">✅ Supported</td>
    <td align="center">API 21+</td>
  </tr>
  <tr>
    <td align="center">🪟 <strong>Windows</strong></td>
    <td align="center">✅ Supported</td>
    <td align="center">10.0.17763.0+</td>
  </tr>
  <tr>
    <td align="center">🍎 <strong>iOS</strong></td>
    <td align="center">🚧 Coming Soon</td>
    <td align="center">15.0+</td>
  </tr>
  <tr>
    <td align="center">💻 <strong>macOS</strong></td>
    <td align="center">🚧 Coming Soon</td>
    <td align="center">Catalyst 15.0+</td>
  </tr>
</table>

> **Note**: Currently supporting Android and Windows. iOS and macOS support coming soon!

## 📥 Download

### Latest Release (v1.0.0)

<table>
  <tr>
    <td align="center"><strong>Platform</strong></td>
    <td align="center"><strong>Download</strong></td>
  </tr>
  <tr>
    <td align="center">🤖 <strong>Android</strong></td>
    <td align="center">
      <a href="https://github.com/Yous2ef/Encryptor/releases/download/v1.0.0/Encryptor-v1.0.0.apk">
        <img src="https://img.shields.io/badge/Download-APK-3DDC84?style=for-the-badge&logo=android" alt="Download APK"/>
      </a>
    </td>
  </tr>
  <tr>
    <td align="center">🪟 <strong>Windows</strong></td>
    <td align="center">
      <a href="https://github.com/Yous2ef/Encryptor/releases/download/v1.0.0/Encryptor-Windows-v1.0.0.zip">
        <img src="https://img.shields.io/badge/Download-ZIP-0078D4?style=for-the-badge&logo=windows" alt="Download ZIP"/>
      </a>
    </td>
  </tr>
  <tr>
    <td align="center">🍎 <strong>iOS/macOS</strong></td>
    <td align="center">
      Coming soon in future release
    </td>
  </tr>
</table>

> **Note**: Currently supporting Android and Windows. iOS and macOS support coming soon!

## 🚀 Getting Started

### Prerequisites

To build from source, you'll need:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Visual Studio 2022 17.8+ or Visual Studio Code with C# extension
- Platform-specific requirements:
  - **Android**: Android SDK (API 34+)
  - **iOS/macOS**: Xcode 15+ (macOS only)
  - **Windows**: Windows 10/11 SDK

### Installation

#### Option 1: Download Pre-built Release
1. Go to the [Releases page](https://github.com/Yous2ef/Encryptor/releases)
2. Download the appropriate package for your platform
3. Install and run

#### Option 2: Build from Source

1. **Clone the repository**
   ```bash
   git clone https://github.com/Yous2ef/Encryptor.git
   cd Encryptor
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build for your platform**

   **Android:**
   ```bash
   dotnet build -f net10.0-android -c Release
   ```

   **Windows:**
   ```bash
   dotnet build -f net10.0-windows10.0.19041.0 -c Release
   ```

   > **iOS/macOS builds coming soon!**

4. **Run the app**
   ```bash
   dotnet run
   ```

## 📖 Usage

### Encrypting Files

1. **Add files**: Tap the `+` button and select files or folders
2. **Enter key**: Type a strong encryption password
3. **Select mode**: Choose "🔐 Encrypt Mode"
4. **Encrypt**: Tap "START ENCRYPTION"

The app will:
- Generate a unique salt and IV for each file
- Encrypt using AES-256-CBC
- Save as `filename.ext.enc`
- Optionally delete the original

### Decrypting Files

1. **Add encrypted files**: Tap `+` to select `.enc` files
2. **Enter key**: Type the same password used for encryption
3. **Select mode**: Choose "🔓 Decrypt Mode"
4. **Decrypt**: Tap "START DECRYPTION"

The app will:
- Decrypt files to memory (no disk I/O)
- Show previews for supported file types
- Allow individual or batch saving

### File Viewer

- **Preview**: Tap any decrypted file to view it
- **Save**: Use the 💾 button to save individual files
- **Save All**: Save all decrypted files to a chosen folder

## 🔧 Technical Details

### Architecture

```
Encryptor/
├── Models/              # Data models (FileModel)
├── ViewModels/          # MVVM ViewModels
├── Views/               # XAML pages
├── Services/            # Business logic
│   ├── EncryptionService    # AES-256 encryption
│   ├── FileService          # File I/O operations
│   └── ScopedDataService    # Shared data between pages
├── Converters/          # Value converters for UI
├── Platforms/           # Platform-specific code
└── Resources/           # Images, fonts, styles
```

### Encryption Specifications

- **Algorithm**: AES-256 in CBC mode
- **Key Derivation**: PBKDF2-HMAC-SHA256
- **Iterations**: 100,000 (OWASP recommendation)
- **Salt Size**: 256 bits (32 bytes)
- **IV Size**: 128 bits (16 bytes)
- **Padding**: PKCS7

**Encrypted File Format:**
```
[32-byte Salt][16-byte IV][Encrypted Data]
```

### Dependencies

- **Microsoft.Maui** - Cross-platform UI framework
- **CommunityToolkit.Mvvm** - MVVM helpers (8.4.0)
- **CommunityToolkit.Maui** - Additional MAUI controls (11.2.0)
- **CommunityToolkit.Maui.MediaElement** - Media playback (4.1.0)

### Performance

- **Streaming Encryption**: Processes files in 1MB chunks to handle large files efficiently
- **Memory Management**: Automatic cleanup of decrypted data when no longer needed
- **Progress Reporting**: Real-time progress updates during operations
- **Cancellation Support**: Cancel long-running operations at any time

## 🛡️ Security Considerations

### ✅ Best Practices Implemented
- Strong encryption (AES-256)
- Secure key derivation (PBKDF2)
- Random salt and IV per file
- Memory cleanup after decryption
- No key storage (user must remember)

### ⚠️ Important Notes
- **Store your password safely**: Lost passwords cannot be recovered
- **Use strong passwords**: Minimum 12 characters, mixed case, numbers, symbols
- **Backup encrypted files**: Keep copies before decryption
- **Secure deletion**: Original files are deleted after encryption (if selected)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [.NET MAUI](https://dotnet.microsoft.com/apps/maui)
- UI components from [CommunityToolkit](https://github.com/CommunityToolkit)
- Inspired by modern encryption tools

## 📞 Contact

**Yous2ef** - [@Yous2ef](https://github.com/Yous2ef)

Project Link: [https://github.com/Yous2ef/Encryptor](https://github.com/Yous2ef/Encryptor)

---

<div align="center">
  Made with ❤️ using .NET MAUI
  <br/>
  <sub>If you find this project useful, please consider giving it a ⭐️</sub>
</div>
