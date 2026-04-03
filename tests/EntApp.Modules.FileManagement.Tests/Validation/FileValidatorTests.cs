using EntApp.Modules.FileManagement.Application.Commands;
using EntApp.Modules.FileManagement.Application.Validators;
using FluentAssertions;

namespace EntApp.Modules.FileManagement.Tests.Validation;

public class FileValidatorTests
{
    [Fact]
    public void UploadFile_Valid_ShouldPass()
    {
        var validator = new UploadFileValidator();
        var cmd = new UploadFileCommand(Stream.Null, "report.pdf", "application/pdf", 1024);

        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UploadFile_EmptyFileName_ShouldFail()
    {
        var validator = new UploadFileValidator();
        var cmd = new UploadFileCommand(Stream.Null, "", "application/pdf", 1024);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public void UploadFile_ZeroSize_ShouldFail()
    {
        var validator = new UploadFileValidator();
        var cmd = new UploadFileCommand(Stream.Null, "file.txt", "text/plain", 0);

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UploadFile_ExceedMaxSize_ShouldFail()
    {
        var validator = new UploadFileValidator();
        var cmd = new UploadFileCommand(Stream.Null, "huge.bin", "application/octet-stream",
            101 * 1024 * 1024); // 101 MB

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SizeInBytes");
    }

    [Fact]
    public void AddTag_Valid_ShouldPass()
    {
        var validator = new AddTagValidator();
        var cmd = new AddTagCommand(Guid.NewGuid(), "finance-2024");

        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddTag_EmptyTagName_ShouldFail()
    {
        var validator = new AddTagValidator();
        var cmd = new AddTagCommand(Guid.NewGuid(), "");

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddTag_InvalidCharacters_ShouldFail()
    {
        var validator = new AddTagValidator();
        var cmd = new AddTagCommand(Guid.NewGuid(), "invalid tag!@#");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddTag_TooLong_ShouldFail()
    {
        var validator = new AddTagValidator();
        var cmd = new AddTagCommand(Guid.NewGuid(), new string('a', 51));

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}
