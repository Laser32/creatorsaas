import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useEffect, useRef } from 'react';
import { apiService } from '../services/apiClient';
import { VideoJob, VideoJobStatus } from '../types';

/**
 * Polls a single video job until it reaches a terminal state,
 * then stops. Returns live video data.
 */
export const useVideoPolling = (videoJobId: string | undefined) => {
  const queryClient = useQueryClient();
  const terminalStates = [VideoJobStatus.Completed, VideoJobStatus.Failed, VideoJobStatus.Cancelled];

  const { data, isLoading, error } = useQuery<VideoJob>({
    queryKey: ['video', videoJobId],
    queryFn: () => apiService.getVideo(videoJobId!).then(r => r.data),
    enabled: !!videoJobId,
    refetchInterval: (query) => {
      const status = query.state.data?.status as VideoJobStatus | undefined;
      if (status === undefined) return 3000;
      return terminalStates.includes(status) ? false : 3000;
    },
  });

  const isProcessing = data ? !terminalStates.includes(data.status) : false;
  const isCompleted = data?.status === VideoJobStatus.Completed;
  const isFailed = data?.status === VideoJobStatus.Failed;

  const progressPercent = data
    ? isCompleted ? 100 : Math.round((data.currentStep / 8) * 100)
    : 0;

  const invalidate = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ['video', videoJobId] });
  }, [queryClient, videoJobId]);

  return { video: data, isLoading, error, isProcessing, isCompleted, isFailed, progressPercent, invalidate };
};

/**
 * Polls the video list for a tenant/project – refreshes every 10s
 * while any video is processing, stops when all are done.
 */
export const useVideoListPolling = (params: Record<string, unknown>) => {
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['videos', params],
    queryFn: () => apiService.listVideos(params).then(r => r.data),
    refetchInterval: (query) => {
      const videos = query.state.data?.data as VideoJob[] | undefined;
      const terminalStates = [VideoJobStatus.Completed, VideoJobStatus.Failed, VideoJobStatus.Cancelled];
      const anyProcessing = videos?.some(v => !terminalStates.includes(v.status));
      return anyProcessing ? 5000 : false;
    },
  });

  return { videos: data?.data ?? [], total: data?.totalCount ?? 0, isLoading, error, refetch };
};

/**
 * WebSocket-like polling hook that triggers a callback when a video
 * transitions from processing → completed.
 */
export const useVideoCompletionCallback = (
  videoJobId: string | undefined,
  onComplete: (video: VideoJob) => void
) => {
  const wasProcessingRef = useRef(false);

  const { video } = useVideoPolling(videoJobId);

  useEffect(() => {
    if (!video) return;

    const isProcessing = video.status < VideoJobStatus.Completed;
    if (wasProcessingRef.current && video.status === VideoJobStatus.Completed) {
      onComplete(video);
    }
    wasProcessingRef.current = isProcessing;
  }, [video, onComplete]);
};
