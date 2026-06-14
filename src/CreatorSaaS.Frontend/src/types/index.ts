// API Response Types
export interface ApiResponse<T> {
  data?: T;
  error?: string;
  message?: string;
  details?: unknown;
  timestamp: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// Auth Types
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
  tenant: Tenant;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'owner' | 'admin' | 'member';
  tenantId: string;
  avatarUrl?: string;
}

export interface Tenant {
  id: string;
  name: string;
  slug: string;
  plan: 'starter' | 'pro' | 'agency';
  monthlyVideoQuota: number;
  videosCreatedThisMonth: number;
}

// Video Types
export enum VideoJobStatus {
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
  Cancelled = 11,
}

export const VideoJobStatusLabels: Record<VideoJobStatus, string> = {
  [VideoJobStatus.Pending]: 'Pending',
  [VideoJobStatus.Queued]: 'Queued',
  [VideoJobStatus.GeneratingScript]: 'Generating Script',
  [VideoJobStatus.GeneratingScenes]: 'Creating Scenes',
  [VideoJobStatus.SynthesizingVoice]: 'Synthesizing Voice',
  [VideoJobStatus.FetchingBRoll]: 'Fetching B-Roll',
  [VideoJobStatus.RenderingVideo]: 'Rendering Video',
  [VideoJobStatus.GeneratingThumbnail]: 'Generating Thumbnail',
  [VideoJobStatus.Uploading]: 'Uploading to YouTube',
  [VideoJobStatus.Completed]: 'Completed',
  [VideoJobStatus.Failed]: 'Failed',
  [VideoJobStatus.Cancelled]: 'Cancelled',
};

export const VideoJobStatusColors: Record<VideoJobStatus, string> = {
  [VideoJobStatus.Pending]: 'gray',
  [VideoJobStatus.Queued]: 'blue',
  [VideoJobStatus.GeneratingScript]: 'purple',
  [VideoJobStatus.GeneratingScenes]: 'indigo',
  [VideoJobStatus.SynthesizingVoice]: 'pink',
  [VideoJobStatus.FetchingBRoll]: 'red',
  [VideoJobStatus.RenderingVideo]: 'orange',
  [VideoJobStatus.GeneratingThumbnail]: 'yellow',
  [VideoJobStatus.Uploading]: 'green',
  [VideoJobStatus.Completed]: 'emerald',
  [VideoJobStatus.Failed]: 'red',
  [VideoJobStatus.Cancelled]: 'slate',
};

export interface VideoScene {
  id: string;
  order: number;
  title: string;
  narration: string;
  brollQuery?: string;
  durationSeconds?: number;
  audioGenerated: boolean;
  brollFetched: boolean;
}

export interface VideoJob {
  id: string;
  projectId: string;
  channelId?: string;
  topic: string;
  keywords?: string;
  language: string;
  style: 'documentary' | 'vlog' | 'tutorial' | 'shorts' | 'news' | 'explainer';
  targetDurationSeconds: number;
  voiceId?: string;
  status: VideoJobStatus;
  currentStep: number;
  errorMessage?: string;
  retryCount: number;
  scriptTitle?: string;
  scriptContent?: string;
  generatedTitle?: string;
  generatedDescription?: string;
  tags?: string;
  youtubeVideoId?: string;
  youtubeUrl?: string;
  views?: number;
  likes?: number;
  comments?: number;
  clickThroughRate?: number;
  createdAt: string;
  completedAt?: string;
  scenes: VideoScene[];
}

export interface VideoJobListItem {
  id: string;
  topic: string;
  scriptTitle?: string;
  status: VideoJobStatus;
  currentStep: number;
  createdAt: string;
  completedAt?: string;
  youtubeUrl?: string;
  views?: number;
}

export interface CreateVideoJobRequest {
  projectId: string;
  channelId?: string;
  topic: string;
  keywords?: string;
  language?: string;
  style?: string;
  targetDurationSeconds?: number;
  voiceId?: string;
}

export interface VideoVariantRequest {
  parentVideoJobId: string;
  variantLabel: string;
  titleOverride?: string;
  descriptionOverride?: string;
}

// Project Types
export interface Project {
  id: string;
  name: string;
  description?: string;
  defaultLanguage?: string;
  defaultVoiceId?: string;
  defaultStyle?: string;
  createdAt: string;
}

export interface CreateProjectRequest {
  name: string;
  description?: string;
  defaultLanguage?: string;
  defaultVoiceId?: string;
  defaultStyle?: string;
}

export interface ProjectAnalytics {
  totalVideos: number;
  totalViews: number;
  totalLikes: number;
  averageClickThroughRate: number;
  averageViewDuration: number;
  topVideos: VideoAnalytics[];
}

export interface VideoAnalytics {
  videoJobId: string;
  views?: number;
  likes?: number;
  comments?: number;
  clickThroughRate?: number;
  averageViewDurationSeconds?: number;
  fetchedAt?: string;
}

// Channel Types
export interface Channel {
  id: string;
  projectId: string;
  name: string;
  isConnected: boolean;
  youtubeChannelId?: string;
  thumbnailUrl?: string;
  totalViews: number;
  totalVideos: number;
}

export interface CreateChannelRequest {
  projectId: string;
  name: string;
}

export interface ConnectYouTubeRequest {
  authorizationCode: string;
  redirectUri: string;
}

// Billing Types
export enum SubscriptionStatus {
  Trialing = 0,
  Active = 1,
  PastDue = 2,
  Canceled = 3,
  Incomplete = 4,
}

export interface Subscription {
  id: string;
  plan: 'starter' | 'pro' | 'agency';
  status: SubscriptionStatus;
  currentPeriodStart?: string;
  currentPeriodEnd?: string;
  cancelAtPeriodEnd: boolean;
  monthlyVideoQuota: number;
  priceMonthly: number;
}

export interface CheckoutSessionRequest {
  priceId: string;
}

export interface CheckoutSession {
  sessionId: string;
  url: string;
}

export interface BillingPortalUrl {
  url: string;
}

// UI State Types
export interface LoadingState {
  isLoading: boolean;
  error?: string;
}

export interface VideoCreateFormState {
  projectId: string;
  channelId?: string;
  topic: string;
  keywords?: string;
  language: string;
  style: string;
  targetDurationSeconds: number;
  voiceId?: string;
}

export interface FilterState {
  projectId?: string;
  channelId?: string;
  status?: VideoJobStatus;
  page: number;
  pageSize: number;
  sortBy: 'created' | 'title' | 'views';
  sortOrder: 'asc' | 'desc';
}

// Error Types
export interface ApiError {
  code: string;
  message: string;
  details?: Record<string, unknown>;
  timestamp: string;
}

export class AppError extends Error {
  constructor(
    public code: string,
    public message: string,
    public statusCode: number = 500,
    public details?: Record<string, unknown>
  ) {
    super(message);
    this.name = 'AppError';
  }
}

// Utility Types
export type AsyncState<T> = 
  | { status: 'idle' }
  | { status: 'pending' }
  | { status: 'success'; data: T }
  | { status: 'error'; error: AppError };

export type FormValidation = Record<string, string[]>;

// Constants
export const VIDEO_STYLES = [
  'documentary',
  'vlog',
  'tutorial',
  'shorts',
  'news',
  'explainer',
] as const;

export const LANGUAGES = [
  { code: 'en', name: 'English' },
  { code: 'es', name: 'Spanish' },
  { code: 'fr', name: 'French' },
  { code: 'de', name: 'German' },
  { code: 'it', name: 'Italian' },
  { code: 'ja', name: 'Japanese' },
  { code: 'zh', name: 'Chinese' },
  { code: 'pt', name: 'Portuguese' },
] as const;

export const PLANS = [
  { id: 'starter', name: 'Starter', price: 19.99, videos: 5 },
  { id: 'pro', name: 'Pro', price: 49.99, videos: 25 },
  { id: 'agency', name: 'Agency', price: 149.99, videos: -1 },
] as const;

export const HANGFIRE_STATUS_MAP: Record<VideoJobStatus, string> = {
  [VideoJobStatus.Pending]: 'Awaiting',
  [VideoJobStatus.Queued]: 'Enqueued',
  [VideoJobStatus.GeneratingScript]: 'Processing',
  [VideoJobStatus.GeneratingScenes]: 'Processing',
  [VideoJobStatus.SynthesizingVoice]: 'Processing',
  [VideoJobStatus.FetchingBRoll]: 'Processing',
  [VideoJobStatus.RenderingVideo]: 'Processing',
  [VideoJobStatus.GeneratingThumbnail]: 'Processing',
  [VideoJobStatus.Uploading]: 'Processing',
  [VideoJobStatus.Completed]: 'Succeeded',
  [VideoJobStatus.Failed]: 'Failed',
  [VideoJobStatus.Cancelled]: 'Deleted',
};
