using Kaarigar.Data;
using Kaarigar.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kaarigar.Services;

public class LearningVideoService : ILearningVideoService
{
    private static readonly string[] AllowedExtensions = { ".mp4", ".webm", ".ogg" };

    private readonly ILearningVideoDao _dao;
    private readonly IJobPostDao _jobPostDao; // reused only for GetActiveSkillsAsync()
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<LearningVideoService> _logger;

    public LearningVideoService(ILearningVideoDao dao, IJobPostDao jobPostDao, IFileStorageService fileStorage, ILogger<LearningVideoService> logger)
    {
        _dao = dao;
        _jobPostDao = jobPostDao;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<LearningVideoAdminViewModel> GetAdminListAsync()
    {
        var videos = await _dao.GetAllAsync();
        var skills = await _jobPostDao.GetActiveSkillsAsync();

        return new LearningVideoAdminViewModel
        {
            Videos = videos.Select(ToRow).ToList(),
            SkillOptions = skills.Select(s => new SelectListItem(s.SkillName, s.SkillId.ToString())).ToList(),
        };
    }

    public async Task<ServiceResult> UploadAsync(int skillId, string title, string? descriptionTxt, IFormFile videoFile, string? adminUser)
    {
        if (string.IsNullOrWhiteSpace(title))
            return new ServiceResult(false, "Title is required.");

        if (videoFile == null || videoFile.Length == 0)
            return new ServiceResult(false, "Please choose a video file.");

        var ext = Path.GetExtension(videoFile.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return new ServiceResult(false, "Only MP4, WEBM or OGG video files are supported.");

        if (videoFile.Length > 200 * 1024 * 1024)
            return new ServiceResult(false, "Video file must be under 200 MB.");

        var (relativePath, _) = await _fileStorage.SaveAsync(videoFile, "learning-videos");

        await _dao.AddAsync(new LearningVideo
        {
            SkillId = skillId,
            Title = title.Trim(),
            DescriptionTxt = descriptionTxt?.Trim(),
            VideoUrl = relativePath,
            IsActiveFl = true,
            CreatedBy = adminUser ?? "ADMIN_LEARNING_VIDEO",
            CreatedTs = DateTime.UtcNow,
        });

        _logger.LogInformation("Learning video uploaded: {Title} (skill {SkillId})", title, skillId);
        return new ServiceResult(true, "Video uploaded.");
    }

    public async Task<ServiceResult> DeleteAsync(int learningVideoId)
    {
        var video = await _dao.GetByIdAsync(learningVideoId);
        if (video == null) return new ServiceResult(false, "Video not found.");

        _fileStorage.Delete(video.VideoUrl);
        await _dao.DeleteAsync(learningVideoId);
        return new ServiceResult(true, "Video deleted.");
    }

    public async Task<ServiceResult> SetActiveAsync(int learningVideoId, bool isActive)
    {
        await _dao.SetActiveAsync(learningVideoId, isActive);
        return new ServiceResult(true, isActive ? "Video shown to employees." : "Video hidden from employees.");
    }

    public async Task<EmployeeLearningViewModel> GetEmployeeVideosAsync(int? skillId)
    {
        var skills = await _jobPostDao.GetActiveSkillsAsync();

        var vm = new EmployeeLearningViewModel
        {
            SkillOptions = skills.Select(s => new SelectListItem(s.SkillName, s.SkillId.ToString())).ToList(),
            SelectedSkillId = skillId,
        };

        if (skillId.HasValue)
        {
            var videos = await _dao.GetActiveBySkillAsync(skillId.Value);
            vm.Videos = videos.Select(ToRow).ToList();
        }

        return vm;
    }

    private static LearningVideoRow ToRow(LearningVideo v) => new()
    {
        LearningVideoId = v.LearningVideoId,
        Title = v.Title,
        SkillName = v.Skill?.SkillName ?? string.Empty,
        VideoUrl = v.VideoUrl,
        IsActiveFl = v.IsActiveFl,
    };
}
