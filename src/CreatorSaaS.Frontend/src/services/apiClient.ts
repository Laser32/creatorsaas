import axios, { AxiosInstance, AxiosError } from 'axios';
import { useAuthStore } from '../store/authStore';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const apiClient: AxiosInstance = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  timeout: 30000,
});

// Request interceptor - add auth token
apiClient.interceptors.request.use((config) => {
  const { accessToken } = useAuthStore.getState();
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

// Response interceptor - handle token refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as any;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      const { refreshToken, setTokens } = useAuthStore.getState();
      if (refreshToken) {
        try {
          const response = await axios.post(`${API_BASE_URL}/api/auth/refresh`, {
            refreshToken,
          });

          const { accessToken, refreshToken: newRefreshToken } = response.data;
          setTokens(accessToken, newRefreshToken);

          originalRequest.headers.Authorization = `Bearer ${accessToken}`;
          return apiClient(originalRequest);
        } catch (refreshError) {
          useAuthStore.getState().logout();
          window.location.href = '/login';
        }
      }
    }

    return Promise.reject(error);
  }
);

// API service methods
export const apiService = {
  // Auth
  login: (email: string, password: string) =>
    apiClient.post('/auth/login', { email, password }),
  register: (tenantName: string, email: string, password: string, firstName: string, lastName: string) =>
    apiClient.post('/auth/register', { tenantName, email, password, firstName, lastName }),
  refreshToken: (refreshToken: string) =>
    apiClient.post('/auth/refresh', { refreshToken }),

  // Videos
  createVideo: (data: any) =>
    apiClient.post('/videos', data),
  getVideo: (id: string) =>
    apiClient.get(`/videos/${id}`),
  listVideos: (params?: any) =>
    apiClient.get('/videos', { params }),
  cancelVideo: (id: string) =>
    apiClient.post(`/videos/${id}/cancel`),
  createVariant: (parentId: string, data: any) =>
    apiClient.post(`/videos/${parentId}/variants`, data),

  // Projects
  createProject: (data: any) =>
    apiClient.post('/projects', data),
  listProjects: () =>
    apiClient.get('/projects'),
  getProject: (id: string) =>
    apiClient.get(`/projects/${id}`),
  getProjectAnalytics: (id: string) =>
    apiClient.get(`/projects/${id}/analytics`),

  // Channels
  createChannel: (data: any) =>
    apiClient.post('/channels', data),
  listChannels: (projectId: string) =>
    apiClient.get('/channels', { params: { projectId } }),
  connectYouTube: (channelId: string, data: any) =>
    apiClient.post(`/channels/${channelId}/youtube/connect`, data),

  // Billing
  getSubscription: () =>
    apiClient.get('/billing/subscription'),
  getCheckoutUrl: (priceId: string) =>
    apiClient.post('/billing/checkout', { priceId }),
  getBillingPortal: () =>
    apiClient.get('/billing/portal'),
};

export default apiClient;
