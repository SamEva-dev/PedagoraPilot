namespace PedagoraPilot.Infrastructure.Storage;
public sealed class ObjectStorageOptions
{
    public const string SectionName = "Storage";
    public string Provider { get; set; } = "FileSystem";
    public string RootPath { get; set; } = "%PEDAGORA_PILOT_HOME%/storage";
    public long MaxFileSizeBytes { get; set; } = 26_214_400;
    public string[] AllowedContentTypes { get; set; } = ["application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/msword", "image/jpeg", "image/png"];
    public string[] AllowedExtensions { get; set; } = [".pdf", ".docx", ".doc", ".jpg", ".jpeg", ".png"];
}
