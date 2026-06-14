namespace CreatorSaaS.Core.Entities;

public class Project : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultLanguage { get; set; } = "en";
    public string? DefaultVoiceId { get; set; }
    public string? DefaultStyle { get; set; }           // documentary | vlog | tutorial | shorts
    public bool IsActive { get; set; } = true;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Channel> Channels { get; set; } = new List<Channel>();
    public ICollection<VideoJob> VideoJobs { get; set; } = new List<VideoJob>();
}
