using EntApp.Modules.Notification.Domain.Entities;
using FluentAssertions;

namespace EntApp.Modules.Notification.Tests.Domain;

public class NotificationTemplateTests
{
    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        var template = NotificationTemplate.Create("WELCOME_EMAIL", "Hoşgeldin E-postası",
            NotificationChannel.Email, "Hoşgeldiniz {{user_name}}", "<h1>Merhaba {{user_name}}</h1>",
            description: "Yeni kullanıcı hoşgeldin e-postası");

        template.Code.Should().Be("WELCOME_EMAIL");
        template.Name.Should().Be("Hoşgeldin E-postası");
        template.Channel.Should().Be(NotificationChannel.Email);
        template.Subject.Should().Contain("{{user_name}}");
        template.Language.Should().Be("tr");
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyCode_ShouldThrow()
    {
        var act = () => NotificationTemplate.Create("", "Name", NotificationChannel.Email, "Sub", "Body");
        act.Should().Throw<ArgumentException>().WithMessage("*kodu*boş*");
    }

    [Fact]
    public void Create_WithEmptySubject_ShouldThrow()
    {
        var act = () => NotificationTemplate.Create("CODE", "Name", NotificationChannel.Email, "", "Body");
        act.Should().Throw<ArgumentException>().WithMessage("*Konu*boş*");
    }

    [Fact]
    public void UpdateContent_ShouldChangeSubjectAndBody()
    {
        var template = NotificationTemplate.Create("CODE", "Name", NotificationChannel.Email, "Old Subject", "Old Body");

        template.UpdateContent("New Subject", "New Body");

        template.Subject.Should().Be("New Subject");
        template.Body.Should().Be("New Body");
    }

    [Fact]
    public void Activate_Deactivate_ShouldToggleState()
    {
        var template = NotificationTemplate.Create("CODE", "Name", NotificationChannel.Email, "Sub", "Body");

        template.Deactivate();
        template.IsActive.Should().BeFalse();

        template.Activate();
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldTrimCode()
    {
        var template = NotificationTemplate.Create("  TRIMMED  ", "Name", NotificationChannel.Email, "Sub", "Body");
        template.Code.Should().Be("TRIMMED");
    }
}

public class NotificationLogTests
{
    [Fact]
    public void Create_ShouldSetPendingStatus()
    {
        var log = NotificationLog.Create(Guid.NewGuid(), "test@test.com",
            NotificationChannel.Email, "Subject", "Body");

        log.Status.Should().Be(NotificationStatus.Pending);
        log.ReadAt.Should().BeNull();
    }

    [Fact]
    public void MarkSent_ShouldSetSentStatus()
    {
        var log = NotificationLog.Create(null, "test@test.com", NotificationChannel.Email, "Sub", "Body");

        log.MarkSent();

        log.Status.Should().Be(NotificationStatus.Sent);
    }

    [Fact]
    public void MarkFailed_ShouldSetFailedStatusAndError()
    {
        var log = NotificationLog.Create(null, "test@test.com", NotificationChannel.Email, "Sub", "Body");

        log.MarkFailed("SMTP timeout");

        log.Status.Should().Be(NotificationStatus.Failed);
        log.ErrorMessage.Should().Be("SMTP timeout");
    }

    [Fact]
    public void MarkRead_ShouldSetReadStatusAndTimestamp()
    {
        var log = NotificationLog.Create(Guid.NewGuid(), "user@test.com", NotificationChannel.InApp, "Sub", "Body");

        log.MarkRead();

        log.Status.Should().Be(NotificationStatus.Read);
        log.ReadAt.Should().NotBeNull();
        log.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithTemplateCode_ShouldSetTemplateCode()
    {
        var log = NotificationLog.Create(null, "test@test.com", NotificationChannel.Email,
            "Sub", "Body", templateCode: "WELCOME_EMAIL");

        log.TemplateCode.Should().Be("WELCOME_EMAIL");
    }
}

public class UserNotificationPreferenceTests
{
    [Fact]
    public void Create_ShouldSetDefaults()
    {
        var userId = Guid.NewGuid();
        var pref = UserNotificationPreference.Create(userId, NotificationChannel.Email);

        pref.UserId.Should().Be(userId);
        pref.Channel.Should().Be(NotificationChannel.Email);
        pref.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Disable_ShouldSetFalse()
    {
        var pref = UserNotificationPreference.Create(Guid.NewGuid(), NotificationChannel.Email);

        pref.Disable();

        pref.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Toggle_ShouldFlipState()
    {
        var pref = UserNotificationPreference.Create(Guid.NewGuid(), NotificationChannel.Sms);

        pref.Toggle();
        pref.IsEnabled.Should().BeFalse();

        pref.Toggle();
        pref.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Create_WithDisabled_ShouldBeDisabled()
    {
        var pref = UserNotificationPreference.Create(Guid.NewGuid(), NotificationChannel.Push, isEnabled: false);

        pref.IsEnabled.Should().BeFalse();
    }
}
