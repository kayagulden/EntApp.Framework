using EntApp.Modules.HR.Application.Commands;
using FluentValidation;

namespace EntApp.Modules.HR.Application.Validators;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.HireDate).NotEmpty();
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => x.Email is not null);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.NationalId).MaximumLength(11);
        RuleFor(x => x.Department).MaximumLength(100);
        RuleFor(x => x.Position).MaximumLength(100);
        RuleFor(x => x.AnnualLeaveEntitlement).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateLeaveRequestCommandValidator : AbstractValidator<CreateLeaveRequestCommand>
{
    public CreateLeaveRequestCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.LeaveType).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty().GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Bitiş tarihi, başlangıç tarihinden önce olamaz");
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public sealed class CreateAttendanceCommandValidator : AbstractValidator<CreateAttendanceCommand>
{
    public CreateAttendanceCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
    }
}

public sealed class ApproveLeaveCommandValidator : AbstractValidator<ApproveLeaveCommand>
{
    public ApproveLeaveCommandValidator()
    {
        RuleFor(x => x.LeaveRequestId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class RejectLeaveCommandValidator : AbstractValidator<RejectLeaveCommand>
{
    public RejectLeaveCommandValidator()
    {
        RuleFor(x => x.LeaveRequestId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
