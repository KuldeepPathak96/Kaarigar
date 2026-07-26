using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IAdminEmployerService
{
    Task<ManageEmployersViewModel> SearchAsync(string? search, string? city, string? status);
    Task<EmployerDetailViewModel?> GetDetailAsync(int userAccountId);
    Task<ServiceResult> ToggleActiveAsync(int userAccountId);

    /// <summary>Admin "Approve" / "Revoke Approval" action — flips USER_ACCOUNT.IS_APPROVED_FL.</summary>
    Task<ServiceResult> ToggleApprovalAsync(int userAccountId);

    Task<ServiceResult> DeleteAsync(int userAccountId);

    /// <summary>Approves the employer's uploaded business proof document (GST/Gumastha).</summary>
    Task<ServiceResult> ApproveBusinessProofAsync(int userAccountId);

    /// <summary>Rejects the employer's uploaded business proof document.</summary>
    Task<ServiceResult> RejectBusinessProofAsync(int userAccountId);
}
