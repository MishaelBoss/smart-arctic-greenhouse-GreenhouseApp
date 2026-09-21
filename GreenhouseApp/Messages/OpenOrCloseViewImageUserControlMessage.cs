using Avalonia.Media.Imaging;

namespace GreenhouseApp.Messages;

public record OpenOrCloseViewImageUserControlMessage(
    Bitmap? LightboxImage = null, 
    string? LightboxTitle = null, 
    double? ZoomScale = null
);