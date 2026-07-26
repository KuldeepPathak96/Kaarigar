using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kaarigar.Models;

/// <summary>Admin-uploaded training video, tied to a Skill. Played in-browser via &lt;video&gt;.</summary>
[Table("LEARNING_VIDEO")]
public class LearningVideo
{
    [Key] [Column("LEARNING_VIDEO_ID")] public int LearningVideoId { get; set; }

    [Required] [Column("SKILL_ID")] public int SkillId { get; set; }

    [Required] [Column("TITLE")] [StringLength(200)] public string Title { get; set; } = string.Empty;

    [Column("DESCRIPTION_TXT")] [StringLength(1000)] public string? DescriptionTxt { get; set; }

    /// <summary>Web-relative path under /uploads/learning-videos/, e.g. "/uploads/learning-videos/xyz.mp4".</summary>
    [Required] [Column("VIDEO_URL")] [StringLength(400)] public string VideoUrl { get; set; } = string.Empty;

    [Column("IS_ACTIVE_FL")] public bool IsActiveFl { get; set; } = true;

    [Column("CREATED_BY")] public string CreatedBy { get; set; } = "SYSTEM";
    [Column("CREATED_TS")] public DateTime CreatedTs { get; set; } = DateTime.UtcNow;
    [Column("UPDATED_BY")] public string? UpdatedBy { get; set; }
    [Column("UPDATED_TS")] public DateTime? UpdatedTs { get; set; }

    [ForeignKey("SkillId")] public Skill? Skill { get; set; }
}

public class LearningVideoAdminViewModel
{
    public List<LearningVideoRow> Videos { get; set; } = new();
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> SkillOptions { get; set; } = new();
}

public class LearningVideoRow
{
    public int LearningVideoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public bool IsActiveFl { get; set; }
}

public class EmployeeLearningViewModel
{
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> SkillOptions { get; set; } = new();
    public int? SelectedSkillId { get; set; }
    public List<LearningVideoRow> Videos { get; set; } = new();
}
