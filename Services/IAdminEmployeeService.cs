using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IAdminEmployeeService
{
    Task<ManageEmployeesViewModel> SearchAsync(string? search, string? city, string? skill, string? status);
    Task<EmployeeDetailViewModel?> GetDetailAsync(int userAccountId);
    Task<ServiceResult> ToggleActiveAsync(int userAccountId);

    /// <summary>Admin "Approve" / "Revoke Approval" action — flips USER_ACCOUNT.IS_APPROVED_FL.</summary>
    Task<ServiceResult> ToggleApprovalAsync(int userAccountId);

    Task<ServiceResult> DeleteAsync(int userAccountId);

    /// <summary>Approves one uploaded EMPLOYEE_DOCUMENT (ID proof or resume).</summary>
    Task<ServiceResult> ApproveDocumentAsync(int employeeDocumentId, int adminUserAccountId);

    /// <summary>Rejects one uploaded EMPLOYEE_DOCUMENT, with an optional reason shown back to the employee.</summary>
    Task<ServiceResult> RejectDocumentAsync(int employeeDocumentId, int adminUserAccountId, string? reason);
}
