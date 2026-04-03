using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.FileManagement.Domain.Entities;

/// <summary>
/// Dosya kaydı — metadata, soft delete, tenant.
/// </summary>
public class FileEntry : AuditableEntity<Guid>
{
    public string FileName { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeInBytes { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public Guid? TenantId { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int CurrentVersion { get; private set; } = 1;

    private readonly List<FileVersion> _versions = new();
    public IReadOnlyCollection<FileVersion> Versions => _versions.AsReadOnly();

    private readonly List<FileTag> _tags = new();
    public IReadOnlyCollection<FileTag> Tags => _tags.AsReadOnly();

    private FileEntry() { }

    public static FileEntry Create(
        string fileName, string originalFileName, string contentType,
        long sizeInBytes, string storagePath,
        string? description = null, string? category = null, Guid? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Dosya adı boş olamaz.", nameof(fileName));
        if (sizeInBytes <= 0)
            throw new ArgumentException("Dosya boyutu 0'dan büyük olmalı.", nameof(sizeInBytes));

        return new FileEntry
        {
            Id = Guid.NewGuid(),
            FileName = fileName.Trim(),
            OriginalFileName = originalFileName,
            ContentType = contentType,
            SizeInBytes = sizeInBytes,
            StoragePath = storagePath,
            Description = description,
            Category = category,
            TenantId = tenantId,
            CurrentVersion = 1
        };
    }

    public FileVersion AddVersion(string storagePath, long sizeInBytes, string? changeNote = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Silinmiş dosyaya versiyon eklenemez.");

        CurrentVersion++;
        var version = FileVersion.Create(Id, CurrentVersion, storagePath, sizeInBytes, changeNote);
        _versions.Add(version);
        StoragePath = storagePath;
        SizeInBytes = sizeInBytes;
        return version;
    }

    public void AddTag(string tagName)
    {
        if (_tags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
            return;
        _tags.Add(FileTag.Create(Id, tagName));
    }

    public void RemoveTag(string tagName)
    {
        var tag = _tags.FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
        if (tag is not null) _tags.Remove(tag);
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }

    public void UpdateMetadata(string? description, string? category)
    {
        Description = description;
        Category = category;
    }

    public string GetExtension() => Path.GetExtension(FileName).TrimStart('.').ToLowerInvariant();

    public bool IsPreviewable()
    {
        var ext = GetExtension();
        return ext is "pdf" or "png" or "jpg" or "jpeg" or "gif" or "webp" or "svg" or "bmp";
    }
}

/// <summary>Dosya versiyonu.</summary>
public class FileVersion : BaseEntity<Guid>
{
    public Guid FileEntryId { get; private set; }
    public int VersionNumber { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public long SizeInBytes { get; private set; }
    public string? ChangeNote { get; private set; }

    private FileVersion() { }

    public static FileVersion Create(Guid fileEntryId, int versionNumber, string storagePath, long sizeInBytes, string? changeNote = null)
    {
        return new FileVersion
        {
            Id = Guid.NewGuid(),
            FileEntryId = fileEntryId,
            VersionNumber = versionNumber,
            StoragePath = storagePath,
            SizeInBytes = sizeInBytes,
            ChangeNote = changeNote
        };
    }
}

/// <summary>Dosya etiketi.</summary>
public class FileTag : BaseEntity<Guid>
{
    public Guid FileEntryId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private FileTag() { }

    public static FileTag Create(Guid fileEntryId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag adı boş olamaz.", nameof(name));

        return new FileTag
        {
            Id = Guid.NewGuid(),
            FileEntryId = fileEntryId,
            Name = name.Trim().ToLowerInvariant()
        };
    }
}
