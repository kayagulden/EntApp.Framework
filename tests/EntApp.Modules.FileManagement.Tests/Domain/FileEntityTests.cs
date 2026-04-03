using EntApp.Modules.FileManagement.Domain.Entities;
using FluentAssertions;

namespace EntApp.Modules.FileManagement.Tests.Domain;

public class FileEntryTests
{
    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        var file = FileEntry.Create("report.pdf", "report.pdf", "application/pdf",
            1024 * 50, "/uploads/report.pdf", description: "Aylık rapor", category: "Reports");

        file.FileName.Should().Be("report.pdf");
        file.ContentType.Should().Be("application/pdf");
        file.SizeInBytes.Should().Be(1024 * 50);
        file.CurrentVersion.Should().Be(1);
        file.IsDeleted.Should().BeFalse();
        file.Category.Should().Be("Reports");
    }

    [Fact]
    public void Create_WithEmptyFileName_ShouldThrow()
    {
        var act = () => FileEntry.Create("", "orig.pdf", "application/pdf", 100, "/path");
        act.Should().Throw<ArgumentException>().WithMessage("*Dosya adı*");
    }

    [Fact]
    public void Create_WithZeroSize_ShouldThrow()
    {
        var act = () => FileEntry.Create("file.txt", "file.txt", "text/plain", 0, "/path");
        act.Should().Throw<ArgumentException>().WithMessage("*boyutu*");
    }

    [Fact]
    public void AddVersion_ShouldIncrementVersion()
    {
        var file = FileEntry.Create("doc.pdf", "doc.pdf", "application/pdf", 1000, "/v1");

        var v2 = file.AddVersion("/v2", 1500, "Düzenleme");

        file.CurrentVersion.Should().Be(2);
        v2.VersionNumber.Should().Be(2);
        v2.ChangeNote.Should().Be("Düzenleme");
        file.StoragePath.Should().Be("/v2");
        file.SizeInBytes.Should().Be(1500);
    }

    [Fact]
    public void AddVersion_WhenDeleted_ShouldThrow()
    {
        var file = FileEntry.Create("doc.pdf", "doc.pdf", "application/pdf", 1000, "/v1");
        file.SoftDelete();

        var act = () => file.AddVersion("/v2", 1500);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Silinmiş*");
    }

    [Fact]
    public void SoftDelete_ShouldSetDeletedState()
    {
        var file = FileEntry.Create("doc.pdf", "doc.pdf", "application/pdf", 1000, "/v1");

        file.SoftDelete();

        file.IsDeleted.Should().BeTrue();
        file.DeletedAt.Should().NotBeNull();
        file.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Restore_ShouldClearDeletedState()
    {
        var file = FileEntry.Create("doc.pdf", "doc.pdf", "application/pdf", 1000, "/v1");
        file.SoftDelete();

        file.Restore();

        file.IsDeleted.Should().BeFalse();
        file.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void AddTag_ShouldAddToCollection()
    {
        var file = FileEntry.Create("doc.pdf", "doc.pdf", "application/pdf", 1000, "/v1");

        file.AddTag("Finance");

        file.Tags.Should().HaveCount(1);
        file.Tags.First().Name.Should().Be("finance"); // lowercase normalized
    }

    [Fact]
    public void AddTag_Duplicate_ShouldNotAdd()
    {
        var file = FileEntry.Create("doc.pdf", "doc.pdf", "application/pdf", 1000, "/v1");
        file.AddTag("Finance");
        file.AddTag("FINANCE");

        file.Tags.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveTag_ShouldRemove()
    {
        var file = FileEntry.Create("doc.pdf", "doc.pdf", "application/pdf", 1000, "/v1");
        file.AddTag("Finance");

        file.RemoveTag("FINANCE"); // case insensitive

        file.Tags.Should().BeEmpty();
    }

    [Fact]
    public void GetExtension_ShouldReturnLowercase()
    {
        var file = FileEntry.Create("Report.PDF", "Report.PDF", "application/pdf", 1000, "/v1");

        file.GetExtension().Should().Be("pdf");
    }

    [Fact]
    public void IsPreviewable_ForPdf_ShouldReturnTrue()
    {
        var file = FileEntry.Create("doc.pdf", "doc.pdf", "application/pdf", 1000, "/v1");
        file.IsPreviewable().Should().BeTrue();
    }

    [Fact]
    public void IsPreviewable_ForJpg_ShouldReturnTrue()
    {
        var file = FileEntry.Create("photo.jpg", "photo.jpg", "image/jpeg", 1000, "/v1");
        file.IsPreviewable().Should().BeTrue();
    }

    [Fact]
    public void IsPreviewable_ForZip_ShouldReturnFalse()
    {
        var file = FileEntry.Create("archive.zip", "archive.zip", "application/zip", 1000, "/v1");
        file.IsPreviewable().Should().BeFalse();
    }

    [Fact]
    public void UpdateMetadata_ShouldUpdateFields()
    {
        var file = FileEntry.Create("doc.pdf", "doc.pdf", "application/pdf", 1000, "/v1");

        file.UpdateMetadata("Updated description", "NewCategory");

        file.Description.Should().Be("Updated description");
        file.Category.Should().Be("NewCategory");
    }
}

public class FileTagTests
{
    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => FileTag.Create(Guid.NewGuid(), "");
        act.Should().Throw<ArgumentException>().WithMessage("*Tag adı*");
    }

    [Fact]
    public void Create_ShouldNormalize()
    {
        var tag = FileTag.Create(Guid.NewGuid(), "  Finance  ");
        tag.Name.Should().Be("finance");
    }
}
