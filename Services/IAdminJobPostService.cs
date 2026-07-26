using Kaarigar.Models;

namespace Kaarigar.Services;

public interface IAdminJobPostService
{
    Task<ManageJobPostsViewModel> SearchAsync(string? search, string? city, string? status, DateTime? fromDate, DateTime? toDate);
    Task<AdminJobPostDetailViewModel?> GetDetailAsync(int jobPostId);
    Task<ServiceResult> CloseJobAsync(int jobPostId);
    Task<ServiceResult> DeleteAsync(int jobPostId);
}
