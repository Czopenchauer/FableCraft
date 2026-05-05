namespace FableCraft.Infrastructure.Images;

public enum ImageGenerationProvider
{
    ComfyUI,
    SwarmUI
}

public sealed class ImageGenerationOptions
{
    public const string SectionName = "ImageGeneration";

    public bool Enabled { get; set; }

    public ImageGenerationProvider Provider { get; set; } = ImageGenerationProvider.ComfyUI;

    public string ImageStoragePath { get; set; } = "./scene-images";

    public string? ImageStorageHostPath { get; set; }

    public string GetEffectiveImageStoragePath() =>
        string.IsNullOrEmpty(ImageStorageHostPath) ? ImageStoragePath : ImageStorageHostPath;
}
