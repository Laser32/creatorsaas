namespace CreatorSaaS.Core.Enums;

public enum VideoJobStatus
{
    Pending = 0,
    Queued = 1,
    GeneratingScript = 2,
    GeneratingScenes = 3,
    SynthesizingVoice = 4,
    FetchingBRoll = 5,
    RenderingVideo = 6,
    GeneratingThumbnail = 7,
    Uploading = 8,
    Completed = 9,
    Failed = 10,
    Cancelled = 11
}

public enum SubscriptionStatus
{
    Trialing = 0,
    Active = 1,
    PastDue = 2,
    Canceled = 3,
    Incomplete = 4
}

public enum VideoStyle
{
    Documentary,
    Vlog,
    Tutorial,
    Shorts,
    NewsReport,
    Explainer
}
