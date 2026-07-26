using Kaarigar.Data;
using Kaarigar.Models;

namespace Kaarigar.Services;

public class AdminJobPostService : IAdminJobPostService
{
    private readonly IAdminJobPostDao _dao;
    private readonly ILogger<AdminJobPostService> _logger;

    public AdminJobPostService(IAdminJobPostDao dao, ILogger<AdminJobPostService> logger)
    {
        _dao = dao;
        _logger = logger;
    }

    public async Task<ManageJobPostsViewModel> SearchAsync(string? search, string? city, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var jobPosts = await _dao.SearchJobPostsAsync(search, city, status, fromDate, toDate);
        var cities = await _dao.GetDistinctCitiesAsync();

        return new ManageJobPostsViewModel
        {
            JobPosts = jobPosts,
            CityOptions = cities,
            SearchTerm = search,
            CityFilter = city,
            StatusFilter = status,
            FromDate = fromDate,
            ToDate = toDate,
        };
    }

    public Task<AdminJobPostDetailViewModel?> GetDetailAsync(int jobPostId) =>
        _dao.GetJobPostDetailAsync(jobPostId);

    public async Task<ServiceResult> CloseJobAsync(int jobPostId)
    {
        var detail = await _dao.GetJobPostDetailAsync(jobPostId);
        if (detail == null)
            return new ServiceResult(false, "Job post not found.");

        if (detail.StatusCd == "CLOSED")
            return new ServiceResult(false, "This job post is already closed.");

        await _dao.CloseJobPostAsync(jobPostId);
        _logger.LogInformation("Admin closed JobPostId={Id}", jobPostId);

        return new ServiceResult(true, "Job post closed.");
    }

    public async Task<ServiceResult> DeleteAsync(int jobPostId)
    {
        var applicantsCount = await _dao.GetApplicantsCountAsync(jobPostId);
        if (applicantsCount > 0)
            return new ServiceResult(false,
                $"Can't delete — this job post has {applicantsCount} applicant(s) on record. Close it instead.");

        await _dao.DeleteJobPostAsync(jobPostId);
        _logger.LogInformation("Admin deleted JobPostId={Id}", jobPostId);

        return new ServiceResult(true, "Job post deleted.");
    }
}
