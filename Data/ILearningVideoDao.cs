using Kaarigar.Models;

namespace Kaarigar.Data;

public interface ILearningVideoDao
{
    Task<List<LearningVideo>> GetAllAsync();
    Task<List<LearningVideo>> GetActiveBySkillAsync(int skillId);
    Task<LearningVideo?> GetByIdAsync(int learningVideoId);
    Task<LearningVideo> AddAsync(LearningVideo video);
    Task DeleteAsync(int learningVideoId);
    Task SetActiveAsync(int learningVideoId, bool isActive);
}
