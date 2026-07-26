using Kaarigar.Models;
using Microsoft.AspNetCore.Http;

namespace Kaarigar.Services;

public interface ILearningVideoService
{
    Task<LearningVideoAdminViewModel> GetAdminListAsync();
    Task<ServiceResult> UploadAsync(int skillId, string title, string? descriptionTxt, IFormFile videoFile, string? adminUser);
    Task<ServiceResult> DeleteAsync(int learningVideoId);
    Task<ServiceResult> SetActiveAsync(int learningVideoId, bool isActive);

    Task<EmployeeLearningViewModel> GetEmployeeVideosAsync(int? skillId);
}
