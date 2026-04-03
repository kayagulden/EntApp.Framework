using EntApp.Modules.Notification.Application.Commands;
using EntApp.Modules.Notification.Application.Validators;
using FluentAssertions;

namespace EntApp.Modules.Notification.Tests.Validation;

public class NotificationValidatorTests
{
    [Fact]
    public void CreateTemplate_Valid_ShouldPass()
    {
        var validator = new CreateTemplateValidator();
        var cmd = new CreateTemplateCommand("WELCOME", "Welcome Email", "Email", "Hello {{name}}", "<p>Welcome</p>");

        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateTemplate_EmptyCode_ShouldFail()
    {
        var validator = new CreateTemplateValidator();
        var cmd = new CreateTemplateCommand("", "Name", "Email", "Subject", "Body");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void CreateTemplate_InvalidChannel_ShouldFail()
    {
        var validator = new CreateTemplateValidator();
        var cmd = new CreateTemplateCommand("CODE", "Name", "InvalidChannel", "Subject", "Body");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Channel");
    }

    [Fact]
    public void SendNotification_Valid_WithTemplate_ShouldPass()
    {
        var validator = new SendNotificationValidator();
        var cmd = new SendNotificationCommand(null, "test@test.com", "Email", TemplateCode: "WELCOME");

        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void SendNotification_Valid_WithSubjectBody_ShouldPass()
    {
        var validator = new SendNotificationValidator();
        var cmd = new SendNotificationCommand(null, "test@test.com", "Email",
            Subject: "Custom Subject", Body: "Custom Body");

        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void SendNotification_EmptyRecipient_ShouldFail()
    {
        var validator = new SendNotificationValidator();
        var cmd = new SendNotificationCommand(null, "", "Email", TemplateCode: "WELCOME");

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SendNotification_NoTemplateNoBody_ShouldFail()
    {
        var validator = new SendNotificationValidator();
        var cmd = new SendNotificationCommand(null, "test@test.com", "Email");

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SendNotification_InvalidChannel_ShouldFail()
    {
        var validator = new SendNotificationValidator();
        var cmd = new SendNotificationCommand(null, "test@test.com", "Pigeon", TemplateCode: "CODE");

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}
