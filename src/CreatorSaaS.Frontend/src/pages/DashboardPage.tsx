import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { apiService } from '../services/apiClient';
import { VideoJobStatus, VideoJobStatusLabels, VideoJobStatusColors } from '../types';
import { Play, Plus, BarChart3, Settings } from 'lucide-react';

const DashboardPage = () => {
  const navigate = useNavigate();
  const { user, tenant, logout } = useAuthStore();

  // Fetch videos
  const { data: videosData, isLoading: videosLoading } = useQuery({
    queryKey: ['videos', { page: 1, pageSize: 10, sortBy: 'created', sortOrder: 'desc' }],
    queryFn: () => apiService.listVideos({
      page: 1,
      pageSize: 10,
      sortBy: 'created',
      sortOrder: 'desc',
    }).then(res => res.data),
    staleTime: 5 * 60 * 1000,
  });

  // Fetch projects
  const { data: projectsData } = useQuery({
    queryKey: ['projects'],
    queryFn: () => apiService.listProjects().then(res => res.data),
    staleTime: 10 * 60 * 1000,
  });

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const getStatusBadgeClass = (status: VideoJobStatus): string => {
    const colors: Record<string, string> = {
      gray: 'bg-gray-100 text-gray-800',
      blue: 'bg-blue-100 text-blue-800',
      purple: 'bg-purple-100 text-purple-800',
      indigo: 'bg-indigo-100 text-indigo-800',
      pink: 'bg-pink-100 text-pink-800',
      red: 'bg-red-100 text-red-800',
      orange: 'bg-orange-100 text-orange-800',
      yellow: 'bg-yellow-100 text-yellow-800',
      green: 'bg-green-100 text-green-800',
      emerald: 'bg-emerald-100 text-emerald-800',
      slate: 'bg-slate-100 text-slate-800',
    };
    return colors[VideoJobStatusColors[status]] || 'bg-gray-100 text-gray-800';
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <h1 className="text-2xl font-bold text-gray-900">CreatorSaaS</h1>
            <span className="px-2 py-1 text-xs font-medium bg-blue-100 text-blue-800 rounded">
              {tenant?.plan.charAt(0).toUpperCase()}{tenant?.plan.slice(1)}
            </span>
          </div>

          <div className="flex items-center space-x-4">
            <button
              onClick={() => navigate('/billing')}
              className="text-gray-600 hover:text-gray-900"
            >
              <BarChart3 className="w-6 h-6" />
            </button>
            <button
              onClick={() => navigate('/settings')}
              className="text-gray-600 hover:text-gray-900"
            >
              <Settings className="w-6 h-6" />
            </button>
            <div className="flex items-center space-x-2 pl-4 border-l border-gray-200">
              <div>
                <p className="text-sm font-medium text-gray-900">{user?.firstName} {user?.lastName}</p>
                <p className="text-xs text-gray-500">{user?.email}</p>
              </div>
              <button
                onClick={handleLogout}
                className="ml-2 text-xs px-2 py-1 text-red-600 hover:bg-red-50 rounded"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Welcome Section */}
        <div className="bg-white rounded-lg shadow p-6 mb-8">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-3xl font-bold text-gray-900 mb-2">
                Welcome back, {user?.firstName}!
              </h2>
              <p className="text-gray-600">
                You have used {tenant?.videosCreatedThisMonth} of {tenant?.monthlyVideoQuota} videos this month
              </p>
            </div>
            <button
              onClick={() => navigate('/videos/new')}
              className="flex items-center space-x-2 px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors"
            >
              <Plus className="w-5 h-5" />
              <span>Create Video</span>
            </button>
          </div>

          {/* Progress Bar */}
          <div className="mt-6">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium text-gray-700">Monthly Quota</span>
              <span className="text-sm text-gray-500">
                {tenant?.videosCreatedThisMonth}/{tenant?.monthlyVideoQuota}
              </span>
            </div>
            <div className="w-full bg-gray-200 rounded-full h-3">
              <div
                className="bg-blue-600 h-3 rounded-full transition-all"
                style={{
                  width: `${((tenant?.videosCreatedThisMonth || 0) / (tenant?.monthlyVideoQuota || 1)) * 100}%`,
                }}
              />
            </div>
          </div>
        </div>

        {/* Projects Quick Links */}
        {projectsData && projectsData.length > 0 && (
          <div className="mb-8">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Projects</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {projectsData.map((project: any) => (
                <div
                  key={project.id}
                  onClick={() => navigate(`/projects/${project.id}`)}
                  className="bg-white rounded-lg shadow p-6 hover:shadow-lg transition-shadow cursor-pointer"
                >
                  <h4 className="font-semibold text-gray-900">{project.name}</h4>
                  <p className="text-sm text-gray-600 mt-1">{project.description}</p>
                  <p className="text-xs text-gray-500 mt-4">
                    {project.language} • {project.defaultStyle}
                  </p>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Recent Videos */}
        <div>
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-gray-900">Recent Videos</h3>
            <button
              onClick={() => navigate('/videos')}
              className="text-blue-600 hover:text-blue-700 text-sm font-medium"
            >
              View All
            </button>
          </div>

          {videosLoading ? (
            <div className="bg-white rounded-lg shadow p-8 text-center">
              <p className="text-gray-600">Loading videos...</p>
            </div>
          ) : !videosData || videosData.data.length === 0 ? (
            <div className="bg-white rounded-lg shadow p-8 text-center">
              <p className="text-gray-600 mb-4">No videos created yet</p>
              <button
                onClick={() => navigate('/videos/new')}
                className="inline-flex items-center space-x-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
              >
                <Plus className="w-4 h-4" />
                <span>Create Your First Video</span>
              </button>
            </div>
          ) : (
            <div className="bg-white rounded-lg shadow overflow-hidden">
              <table className="w-full">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                      Title
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                      Views
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                      Created
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                      Action
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {videosData.data.map((video: any) => (
                    <tr key={video.id} className="hover:bg-gray-50">
                      <td className="px-6 py-4">
                        <p className="font-medium text-gray-900">
                          {video.scriptTitle || video.topic}
                        </p>
                      </td>
                      <td className="px-6 py-4">
                        <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded ${getStatusBadgeClass(video.status)}`}>
                          {VideoJobStatusLabels[video.status]}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-gray-600">
                        {video.views ? video.views.toLocaleString() : '—'}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-600">
                        {new Date(video.createdAt).toLocaleDateString()}
                      </td>
                      <td className="px-6 py-4">
                        <button
                          onClick={() => navigate(`/videos/${video.id}`)}
                          className="text-blue-600 hover:text-blue-700"
                        >
                          <Play className="w-4 h-4" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </main>
    </div>
  );
};

export default DashboardPage;
