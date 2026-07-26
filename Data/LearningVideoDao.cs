using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class LearningVideoDao : ILearningVideoDao
{
    private readonly AppDbContext _db;

    public LearningVideoDao(AppDbContext db) => _db = db;

    public Task<List<LearningVideo>> GetAllAsync() =>
        _db.LearningVideos.AsNoTracking().Include(v => v.Skill).OrderByDescending(v => v.CreatedTs).ToListAsync();

    public Task<List<LearningVideo>> GetActiveBySkillAsync(int skillId) =>
        _db.LearningVideos.AsNoTracking().Include(v => v.Skill)
           .Where(v => v.SkillId == skillId && v.IsActiveFl)
           .OrderByDescending(v => v.CreatedTs).ToListAsync();

    public Task<LearningVideo?> GetByIdAsync(int learningVideoId) =>
        _db.LearningVideos.FirstOrDefaultAsync(v => v.LearningVideoId == learningVideoId);

    public async Task<LearningVideo> AddAsync(LearningVideo video)
    {
        _db.LearningVideos.Add(video);
        await _db.SaveChangesAsync();
        return video;
    }

    public async Task DeleteAsync(int learningVideoId)
    {
        var video = await _db.LearningVideos.FindAsync(learningVideoId);
        if (video == null) return;
        _db.LearningVideos.Remove(video);
        await _db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int learningVideoId, bool isActive)
    {
        var video = await _db.LearningVideos.FindAsync(learningVideoId);
        if (video == null) return;
        video.IsActiveFl = isActive;
        video.UpdatedBy = "ADMIN_LEARNING_VIDEO";
        video.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
