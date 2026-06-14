import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiService } from '../services/apiClient';
import { VIDEO_STYLES, LANGUAGES } from '../types';
import { ArrowLeft, Loader2, Wand2 } from 'lucide-react';

const ELEVENLABS_VOICES = [
  { id: '21m00Tcm4TlvDq8ikWAM', name: 'Rachel (English, Female)' },
  { id: 'AZnzlk1XvdvUeBnXmlld', name: 'Domi (English, Female)' },
  { id: 'EXAVITQu4vr4xnSDxMaL', name: 'Bella (English, Female)' },
  { id: 'ErXwobaYiN019PkySvjV', name: 'Antoni (English, Male)' },
  { id: 'MF3mGyEYCl7XYWbV9V6O', name: 'Elli (English, Female)' },
  { id: 'TxGEqnHWrfWFTfGW9XjX', name: 'Josh (English, Male)' },
  { id: 'VR6AewLTigWG4xSOukaG', name: 'Arnold (English, Male)' },
  { id: 'pNInz6obpgDQGcFmaJgB', name: 'Adam (English, Male)' },
  { id: 'yoZ06aMxZJJ28mfd3POQ', name: 'Sam (English, Male)' },
];

const VideoCreatePage = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [form, setForm] = useState({
    projectId: '',
    channelId: '',
    topic: '',
    keywords: '',
    language: 'en',
    style: 'tutorial',
    targetDurationSeconds: 300,
    voiceId: '21m00Tcm4TlvDq8ikWAM',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Fetch projects
  const { data: projects } = useQuery({
    queryKey: ['projects'],
    queryFn: () => apiService.listProjects().then(r => r.data),
  });

  // Fetch channels for selected project
  const { data: channels } = useQuery({
    queryKey: ['channels', form.projectId],
    queryFn: () => apiService.listChannels(form.projectId).then(r => r.data),
    enabled: !!form.projectId,
  });

  // Create video mutation
  const createMutation = useMutation({
    mutationFn: (data: typeof form) => apiService.createVideo({
      ...data,
      channelId: data.channelId || undefined,
    }),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['videos'] });
      navigate(`/videos/${res.data.id}`);
    },
    onError: (err: any) => {
      const message = err.response?.data?.error || 'Failed to create video';
      setErrors({ submit: message });
    },
  });

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};
    if (!form.projectId) newErrors.projectId = 'Please select a project';
    if (!form.topic.trim()) newErrors.topic = 'Topic is required';
    if (form.topic.length < 10) newErrors.topic = 'Topic must be at least 10 characters';
    if (form.targetDurationSeconds < 30) newErrors.duration = 'Minimum duration is 30 seconds';
    if (form.targetDurationSeconds > 1800) newErrors.duration = 'Maximum duration is 30 minutes';
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validate()) createMutation.mutate(form);
  };

  const durationOptions = [
    { value: 60, label: '1 minute (Short)' },
    { value: 180, label: '3 minutes' },
    { value: 300, label: '5 minutes (Recommended)' },
    { value: 600, label: '10 minutes' },
    { value: 900, label: '15 minutes' },
    { value: 1200, label: '20 minutes (Long)' },
  ];

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-4xl mx-auto px-4 py-4 flex items-center space-x-4">
          <button
            onClick={() => navigate('/dashboard')}
            className="text-gray-500 hover:text-gray-700 transition-colors"
          >
            <ArrowLeft className="w-5 h-5" />
          </button>
          <div>
            <h1 className="text-xl font-bold text-gray-900">Create New Video</h1>
            <p className="text-sm text-gray-500">AI will generate script, voice, B-roll and upload to YouTube</p>
          </div>
        </div>
      </header>

      <main className="max-w-4xl mx-auto px-4 py-8">
        <form onSubmit={handleSubmit} className="space-y-6">

          {/* Global error */}
          {errors.submit && (
            <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
              {errors.submit}
            </div>
          )}

          {/* Project & Channel */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Project Settings</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Project <span className="text-red-500">*</span>
                </label>
                <select
                  value={form.projectId}
                  onChange={e => setForm({ ...form, projectId: e.target.value, channelId: '' })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Select a project...</option>
                  {projects?.map((p: any) => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </select>
                {errors.projectId && (
                  <p className="text-red-500 text-xs mt-1">{errors.projectId}</p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  YouTube Channel <span className="text-gray-400">(optional)</span>
                </label>
                <select
                  value={form.channelId}
                  onChange={e => setForm({ ...form, channelId: e.target.value })}
                  disabled={!form.projectId}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
                >
                  <option value="">No channel (don't upload)</option>
                  {channels?.map((c: any) => (
                    <option key={c.id} value={c.id}>
                      {c.name} {c.isConnected ? '✅' : '(not connected)'}
                    </option>
                  ))}
                </select>
                {!form.projectId && (
                  <p className="text-gray-400 text-xs mt-1">Select a project first</p>
                )}
              </div>
            </div>
          </div>

          {/* Topic & Keywords */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Video Content</h2>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Video Topic <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={form.topic}
                  onChange={e => setForm({ ...form, topic: e.target.value })}
                  placeholder="e.g. 10 Best Productivity Tips for Remote Workers in 2024"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                {errors.topic && (
                  <p className="text-red-500 text-xs mt-1">{errors.topic}</p>
                )}
                <p className="text-gray-400 text-xs mt-1">
                  Be specific – the more detail, the better the script
                </p>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Keywords <span className="text-gray-400">(optional)</span>
                </label>
                <input
                  type="text"
                  value={form.keywords}
                  onChange={e => setForm({ ...form, keywords: e.target.value })}
                  placeholder="e.g. productivity, remote work, time management, work from home"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <p className="text-gray-400 text-xs mt-1">
                  Comma-separated keywords for SEO optimization
                </p>
              </div>
            </div>
          </div>

          {/* Style, Language, Duration */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Video Style</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">

              {/* Style */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Style</label>
                <div className="grid grid-cols-2 gap-2">
                  {VIDEO_STYLES.map(style => (
                    <button
                      key={style}
                      type="button"
                      onClick={() => setForm({ ...form, style })}
                      className={`px-3 py-2 text-sm rounded-lg border transition-colors ${
                        form.style === style
                          ? 'bg-blue-600 text-white border-blue-600'
                          : 'bg-white text-gray-700 border-gray-300 hover:border-blue-400'
                      }`}
                    >
                      {style.charAt(0).toUpperCase() + style.slice(1)}
                    </button>
                  ))}
                </div>
              </div>

              {/* Language */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Language</label>
                <select
                  value={form.language}
                  onChange={e => setForm({ ...form, language: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  {LANGUAGES.map(l => (
                    <option key={l.code} value={l.code}>{l.name}</option>
                  ))}
                </select>
              </div>

              {/* Duration */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Duration</label>
                <select
                  value={form.targetDurationSeconds}
                  onChange={e => setForm({ ...form, targetDurationSeconds: Number(e.target.value) })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  {durationOptions.map(d => (
                    <option key={d.value} value={d.value}>{d.label}</option>
                  ))}
                </select>
                {errors.duration && (
                  <p className="text-red-500 text-xs mt-1">{errors.duration}</p>
                )}
              </div>
            </div>
          </div>

          {/* Voice Selection */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Voice</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-2">
              {ELEVENLABS_VOICES.map(voice => (
                <button
                  key={voice.id}
                  type="button"
                  onClick={() => setForm({ ...form, voiceId: voice.id })}
                  className={`px-3 py-2 text-sm rounded-lg border text-left transition-colors ${
                    form.voiceId === voice.id
                      ? 'bg-blue-600 text-white border-blue-600'
                      : 'bg-white text-gray-700 border-gray-300 hover:border-blue-400'
                  }`}
                >
                  {voice.name}
                </button>
              ))}
            </div>
          </div>

          {/* Pipeline Preview */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Processing Pipeline</h2>
            <div className="flex flex-wrap gap-2">
              {[
                'Generate Script',
                'Create Scenes',
                'Synthesize Voice',
                'Fetch B-Roll',
                'Render Video',
                'Generate Thumbnail',
                form.channelId ? 'Upload to YouTube' : 'Save Video',
              ].map((step, i) => (
                <div key={i} className="flex items-center space-x-1">
                  <span className="w-6 h-6 rounded-full bg-blue-100 text-blue-700 text-xs flex items-center justify-center font-bold">
                    {i + 1}
                  </span>
                  <span className="text-sm text-gray-700">{step}</span>
                  {i < 6 && <span className="text-gray-400 ml-1">→</span>}
                </div>
              ))}
            </div>
            <p className="text-xs text-gray-400 mt-3">
              Estimated processing time: {Math.ceil(form.targetDurationSeconds / 60 * 2)}–{Math.ceil(form.targetDurationSeconds / 60 * 4)} minutes
            </p>
          </div>

          {/* Submit */}
          <div className="flex items-center justify-between">
            <button
              type="button"
              onClick={() => navigate('/dashboard')}
              className="px-6 py-3 border border-gray-300 text-gray-700 font-medium rounded-lg hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createMutation.isPending}
              className="flex items-center space-x-2 px-8 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors disabled:bg-gray-400 disabled:cursor-not-allowed"
            >
              {createMutation.isPending ? (
                <>
                  <Loader2 className="w-5 h-5 animate-spin" />
                  <span>Creating...</span>
                </>
              ) : (
                <>
                  <Wand2 className="w-5 h-5" />
                  <span>Create AI Video</span>
                </>
              )}
            </button>
          </div>
        </form>
      </main>
    </div>
  );
};

export default VideoCreatePage;
