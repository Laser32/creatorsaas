import { useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiService } from '../services/apiClient';
import { VideoJobStatus, VideoJobStatusLabels, VideoJobStatusColors } from '../types';
import {
  ArrowLeft, ExternalLink, Copy, XCircle, GitBranch,
  Eye, ThumbsUp, MessageSquare, BarChart2, Loader2,
  CheckCircle, AlertCircle, Clock
} from 'lucide-react';

const PIPELINE_STEPS = [
  { step: 2, label: 'Generating Script', icon: '✍️' },
  { step: 3, label: 'Creating Scenes', icon: '🎬' },
  { step: 4, label: 'Synthesizing Voice', icon: '🎙️' },
  { step: 5, label: 'Fetching B-Roll', icon: '🎥' },
  { step: 6, label: 'Rendering Video', icon: '🖥️' },
  { step: 7, label: 'Generating Thumbnail', icon: '🖼️' },
  { step: 8, label: 'Uploading to YouTube', icon: '🚀' },
];

const VideoDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const { data: video, isLoading, error } = useQuery({
    queryKey: ['video', id],
    queryFn: () => apiService.getVideo(id!).then(r => r.data),
    enabled: !!id,
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      // Auto-poll while processing
      if (status !== undefined && status < VideoJobStatus.Completed) return 3000;
      return false;
    },
  });

  // Cancel mutation
  const cancelMutation = useMutation({
    mutationFn: () => apiService.cancelVideo(id!),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['video', id] }),
  });

  const isProcessing = video && video.status < VideoJobStatus.Completed;
  const isCompleted = video?.status === VideoJobStatus.Completed;
  const isFailed = video?.status === VideoJobStatus.Failed;

  const getProgressPercent = () => {
    if (!video) return 0;
    if (isCompleted) return 100;
    if (isFailed) return 0;
    const maxStep = 8;
    return Math.round((video.currentStep / maxStep) * 100);
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
  };

  if (isLoading) return (
    <div className="min-h-screen flex items-center justify-center">
      <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
    </div>
  );

  if (error || !video) return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-4">
      <AlertCircle className="w-12 h-12 text-red-500" />
      <p className="text-gray-700">Video not found</p>
      <button onClick={() => navigate('/dashboard')} className="text-blue-600 hover:underline">
        Back to Dashboard
      </button>
    </div>
  );

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-5xl mx-auto px-4 py-4 flex items-center justify-between">
          <div className="flex items-center space-x-4">
            <button onClick={() => navigate('/dashboard')} className="text-gray-500 hover:text-gray-700">
              <ArrowLeft className="w-5 h-5" />
            </button>
            <div>
              <h1 className="text-xl font-bold text-gray-900 truncate max-w-xl">
                {video.generatedTitle || video.scriptTitle || video.topic}
              </h1>
              <p className="text-sm text-gray-500">
                Created {new Date(video.createdAt).toLocaleString()}
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-2">
            {/* Status badge */}
            <span className={`px-3 py-1 rounded-full text-sm font-medium bg-${VideoJobStatusColors[video.status]}-100 text-${VideoJobStatusColors[video.status]}-800`}>
              {isProcessing && <Loader2 className="inline w-3 h-3 mr-1 animate-spin" />}
              {isCompleted && <CheckCircle className="inline w-3 h-3 mr-1" />}
              {isFailed && <AlertCircle className="inline w-3 h-3 mr-1" />}
              {VideoJobStatusLabels[video.status]}
            </span>

            {/* Cancel button */}
            {isProcessing && (
              <button
                onClick={() => cancelMutation.mutate()}
                disabled={cancelMutation.isPending}
                className="flex items-center space-x-1 px-3 py-1 text-sm text-red-600 border border-red-300 rounded-lg hover:bg-red-50"
              >
                <XCircle className="w-4 h-4" />
                <span>Cancel</span>
              </button>
            )}
          </div>
        </div>
      </header>

      <main className="max-w-5xl mx-auto px-4 py-8 space-y-6">

        {/* Processing Status */}
        {isProcessing && (
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-gray-900">Processing Pipeline</h2>
              <span className="text-sm text-gray-500">{getProgressPercent()}% complete</span>
            </div>

            {/* Progress bar */}
            <div className="w-full bg-gray-200 rounded-full h-3 mb-6">
              <div
                className="bg-blue-600 h-3 rounded-full transition-all duration-500"
                style={{ width: `${getProgressPercent()}%` }}
              />
            </div>

            {/* Pipeline steps */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              {PIPELINE_STEPS.map(({ step, label, icon }) => {
                const isDone = video.currentStep >= step && isProcessing;
                const isCurrent = video.status === step;
                const isPending = video.currentStep < step;
                return (
                  <div
                    key={step}
                    className={`flex items-center space-x-2 p-3 rounded-lg border ${
                      isCurrent ? 'border-blue-500 bg-blue-50' :
                      isDone ? 'border-green-300 bg-green-50' :
                      'border-gray-200 bg-gray-50'
                    }`}
                  >
                    <span className="text-lg">{icon}</span>
                    <div>
                      <p className={`text-xs font-medium ${
                        isCurrent ? 'text-blue-700' :
                        isDone ? 'text-green-700' :
                        'text-gray-500'
                      }`}>
                        {isCurrent && <Loader2 className="inline w-3 h-3 mr-1 animate-spin" />}
                        {isDone && !isCurrent && '✓ '}
                        {label}
                      </p>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        )}

        {/* Failed State */}
        {isFailed && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-6">
            <div className="flex items-start space-x-3">
              <AlertCircle className="w-6 h-6 text-red-500 mt-0.5" />
              <div>
                <h3 className="font-semibold text-red-800">Video Processing Failed</h3>
                <p className="text-red-700 text-sm mt-1">{video.errorMessage || 'An unknown error occurred'}</p>
                <p className="text-red-500 text-xs mt-2">Retry count: {video.retryCount}</p>
                <button
                  onClick={() => navigate(`/videos/new`)}
                  className="mt-3 px-4 py-2 bg-red-600 text-white text-sm rounded-lg hover:bg-red-700"
                >
                  Create New Video
                </button>
              </div>
            </div>
          </div>
        )}

        {/* YouTube Link */}
        {isCompleted && video.youtubeUrl && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-6">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-3">
                <span className="text-2xl">🎉</span>
                <div>
                  <h3 className="font-semibold text-gray-900">Your video is live on YouTube!</h3>
                  <a
                    href={video.youtubeUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-red-600 hover:text-red-700 text-sm font-medium"
                  >
                    {video.youtubeUrl}
                  </a>
                </div>
              </div>
              <div className="flex space-x-2">
                <button
                  onClick={() => copyToClipboard(video.youtubeUrl!)}
                  className="p-2 text-gray-500 hover:text-gray-700 border border-gray-200 rounded-lg"
                  title="Copy URL"
                >
                  <Copy className="w-4 h-4" />
                </button>
                <a
                  href={video.youtubeUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center space-x-1 px-4 py-2 bg-red-600 text-white text-sm rounded-lg hover:bg-red-700"
                >
                  <ExternalLink className="w-4 h-4" />
                  <span>Watch on YouTube</span>
                </a>
              </div>
            </div>
          </div>
        )}

        {/* Analytics */}
        {isCompleted && (video.views !== null || video.likes !== null) && (
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center space-x-2 mb-4">
              <BarChart2 className="w-5 h-5 text-blue-600" />
              <h2 className="text-lg font-semibold text-gray-900">Analytics</h2>
            </div>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="text-center p-4 bg-blue-50 rounded-lg">
                <Eye className="w-6 h-6 text-blue-600 mx-auto mb-2" />
                <p className="text-2xl font-bold text-gray-900">{video.views?.toLocaleString() ?? '—'}</p>
                <p className="text-sm text-gray-500">Views</p>
              </div>
              <div className="text-center p-4 bg-green-50 rounded-lg">
                <ThumbsUp className="w-6 h-6 text-green-600 mx-auto mb-2" />
                <p className="text-2xl font-bold text-gray-900">{video.likes?.toLocaleString() ?? '—'}</p>
                <p className="text-sm text-gray-500">Likes</p>
              </div>
              <div className="text-center p-4 bg-purple-50 rounded-lg">
                <MessageSquare className="w-6 h-6 text-purple-600 mx-auto mb-2" />
                <p className="text-2xl font-bold text-gray-900">{video.comments?.toLocaleString() ?? '—'}</p>
                <p className="text-sm text-gray-500">Comments</p>
              </div>
              <div className="text-center p-4 bg-orange-50 rounded-lg">
                <BarChart2 className="w-6 h-6 text-orange-600 mx-auto mb-2" />
                <p className="text-2xl font-bold text-gray-900">
                  {video.clickThroughRate ? `${video.clickThroughRate.toFixed(1)}%` : '—'}
                </p>
                <p className="text-sm text-gray-500">CTR</p>
              </div>
            </div>
          </div>
        )}

        {/* Script / Scenes */}
        {video.scenes && video.scenes.length > 0 && (
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
              Script Scenes ({video.scenes.length})
            </h2>
            <div className="space-y-4">
              {video.scenes.map((scene) => (
                <div key={scene.id} className="border border-gray-200 rounded-lg p-4">
                  <div className="flex items-center justify-between mb-2">
                    <div className="flex items-center space-x-2">
                      <span className="w-7 h-7 rounded-full bg-blue-100 text-blue-700 text-sm flex items-center justify-center font-bold">
                        {scene.order}
                      </span>
                      <h3 className="font-medium text-gray-900">{scene.title}</h3>
                    </div>
                    <div className="flex items-center space-x-2 text-xs text-gray-500">
                      {scene.audioGenerated && (
                        <span className="px-2 py-0.5 bg-green-100 text-green-700 rounded">🎙️ Audio</span>
                      )}
                      {scene.brollFetched && (
                        <span className="px-2 py-0.5 bg-blue-100 text-blue-700 rounded">🎥 B-Roll</span>
                      )}
                      {scene.durationSeconds && (
                        <span className="flex items-center">
                          <Clock className="w-3 h-3 mr-1" />{scene.durationSeconds}s
                        </span>
                      )}
                    </div>
                  </div>
                  <p className="text-sm text-gray-600 mt-2 leading-relaxed">{scene.narration}</p>
                  {scene.brollQuery && (
                    <p className="text-xs text-gray-400 mt-1">B-Roll: "{scene.brollQuery}"</p>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Meta Info */}
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Video Details</h2>
          <dl className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
            {[
              { label: 'Topic', value: video.topic },
              { label: 'Style', value: video.style },
              { label: 'Language', value: video.language },
              { label: 'Target Duration', value: `${video.targetDurationSeconds}s` },
              video.generatedTitle && { label: 'Generated Title', value: video.generatedTitle },
              video.tags && { label: 'Tags', value: video.tags },
              video.generatedDescription && { label: 'Description', value: video.generatedDescription },
            ].filter(Boolean).map((item: any) => (
              <div key={item.label} className="flex flex-col">
                <dt className="text-gray-500 font-medium">{item.label}</dt>
                <dd className="text-gray-900 mt-0.5">{item.value}</dd>
              </div>
            ))}
          </dl>
        </div>

        {/* A/B Variant Button */}
        {isCompleted && (
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-lg font-semibold text-gray-900">A/B Testing</h2>
                <p className="text-sm text-gray-500 mt-1">
                  Create a variant with a different title or description to test performance
                </p>
              </div>
              <button
                onClick={() => navigate(`/videos/new?variant=${id}`)}
                className="flex items-center space-x-2 px-4 py-2 border border-blue-300 text-blue-600 rounded-lg hover:bg-blue-50 transition-colors"
              >
                <GitBranch className="w-4 h-4" />
                <span>Create Variant</span>
              </button>
            </div>
          </div>
        )}
      </main>
    </div>
  );
};

export default VideoDetailPage;
