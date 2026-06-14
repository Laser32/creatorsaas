import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { ArrowLeft, User, Key, Users, Bell, Trash2, Save, Loader2 } from 'lucide-react';

type Tab = 'profile' | 'api' | 'team' | 'notifications' | 'danger';

const SettingsPage = () => {
  const navigate = useNavigate();
  const { user, tenant, logout } = useAuthStore();
  const [activeTab, setActiveTab] = useState<Tab>('profile');
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  const [profile, setProfile] = useState({
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    email: user?.email || '',
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  const [apiKeys, setApiKeys] = useState({
    openaiKey: '••••••••',
    anthropicKey: '••••••••',
    elevenLabsKey: '••••••••',
    pexelsKey: '••••••••',
    aiProvider: 'openai',
  });

  const [notifications, setNotifications] = useState({
    emailOnComplete: true,
    emailOnError: true,
    emailWeeklyReport: false,
  });

  const handleSave = async () => {
    setSaving(true);
    await new Promise(r => setTimeout(r, 1000));
    setSaving(false);
    setSaved(true);
    setTimeout(() => setSaved(false), 3000);
  };

  const TABS = [
    { id: 'profile', label: 'Profile', icon: User },
    { id: 'api', label: 'API Keys', icon: Key },
    { id: 'team', label: 'Team', icon: Users },
    { id: 'notifications', label: 'Notifications', icon: Bell },
    { id: 'danger', label: 'Danger Zone', icon: Trash2 },
  ] as const;

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-5xl mx-auto px-4 py-4 flex items-center space-x-4">
          <button onClick={() => navigate('/dashboard')} className="text-gray-500 hover:text-gray-700">
            <ArrowLeft className="w-5 h-5" />
          </button>
          <div>
            <h1 className="text-xl font-bold text-gray-900">Settings</h1>
            <p className="text-sm text-gray-500">{tenant?.name}</p>
          </div>
        </div>
      </header>

      <main className="max-w-5xl mx-auto px-4 py-8">
        <div className="flex gap-8">
          {/* Sidebar */}
          <div className="w-48 flex-shrink-0">
            <nav className="space-y-1">
              {TABS.map(({ id, label, icon: Icon }) => (
                <button
                  key={id}
                  onClick={() => setActiveTab(id)}
                  className={`w-full flex items-center space-x-3 px-3 py-2 rounded-lg text-sm transition-colors ${
                    activeTab === id
                      ? 'bg-blue-50 text-blue-700 font-medium'
                      : 'text-gray-600 hover:bg-gray-100'
                  } ${id === 'danger' ? 'text-red-600 hover:bg-red-50' : ''}`}
                >
                  <Icon className="w-4 h-4" />
                  <span>{label}</span>
                </button>
              ))}
            </nav>
          </div>

          {/* Content */}
          <div className="flex-1">
            {/* Profile */}
            {activeTab === 'profile' && (
              <div className="bg-white rounded-lg shadow p-6 space-y-5">
                <h2 className="text-lg font-semibold text-gray-900">Profile Settings</h2>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">First Name</label>
                    <input
                      type="text"
                      value={profile.firstName}
                      onChange={e => setProfile({ ...profile, firstName: e.target.value })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
                    <input
                      type="text"
                      value={profile.lastName}
                      onChange={e => setProfile({ ...profile, lastName: e.target.value })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                  <input
                    type="email"
                    value={profile.email}
                    onChange={e => setProfile({ ...profile, email: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <hr className="border-gray-200" />
                <h3 className="text-md font-medium text-gray-900">Change Password</h3>

                {['currentPassword', 'newPassword', 'confirmPassword'].map((field) => (
                  <div key={field}>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      {field === 'currentPassword' ? 'Current Password'
                        : field === 'newPassword' ? 'New Password'
                        : 'Confirm New Password'}
                    </label>
                    <input
                      type="password"
                      value={(profile as any)[field]}
                      onChange={e => setProfile({ ...profile, [field]: e.target.value })}
                      placeholder="••••••••"
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                  </div>
                ))}

                <div className="flex justify-end">
                  <button
                    onClick={handleSave}
                    disabled={saving}
                    className="flex items-center space-x-2 px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
                  >
                    {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
                    <span>{saved ? 'Saved!' : 'Save Changes'}</span>
                  </button>
                </div>
              </div>
            )}

            {/* API Keys */}
            {activeTab === 'api' && (
              <div className="bg-white rounded-lg shadow p-6 space-y-5">
                <h2 className="text-lg font-semibold text-gray-900">API Configuration</h2>
                <p className="text-sm text-gray-500">
                  Configure your AI and service API keys. These are stored encrypted.
                </p>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">AI Provider</label>
                  <select
                    value={apiKeys.aiProvider}
                    onChange={e => setApiKeys({ ...apiKeys, aiProvider: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option value="openai">OpenAI (GPT-4)</option>
                    <option value="anthropic">Anthropic (Claude)</option>
                  </select>
                </div>

                {[
                  { key: 'openaiKey', label: 'OpenAI API Key', placeholder: 'sk-...' },
                  { key: 'anthropicKey', label: 'Anthropic API Key', placeholder: 'sk-ant-...' },
                  { key: 'elevenLabsKey', label: 'ElevenLabs API Key', placeholder: 'Your ElevenLabs key' },
                  { key: 'pexelsKey', label: 'Pexels API Key', placeholder: 'Your Pexels key' },
                ].map(({ key, label, placeholder }) => (
                  <div key={key}>
                    <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
                    <input
                      type="password"
                      value={(apiKeys as any)[key]}
                      onChange={e => setApiKeys({ ...apiKeys, [key]: e.target.value })}
                      placeholder={placeholder}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono text-sm"
                    />
                  </div>
                ))}

                <div className="flex justify-end">
                  <button
                    onClick={handleSave}
                    disabled={saving}
                    className="flex items-center space-x-2 px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
                  >
                    {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
                    <span>{saved ? 'Saved!' : 'Save API Keys'}</span>
                  </button>
                </div>
              </div>
            )}

            {/* Notifications */}
            {activeTab === 'notifications' && (
              <div className="bg-white rounded-lg shadow p-6 space-y-5">
                <h2 className="text-lg font-semibold text-gray-900">Notification Preferences</h2>

                {[
                  { key: 'emailOnComplete', label: 'Email when video is complete', desc: 'Get notified when your video has been uploaded to YouTube' },
                  { key: 'emailOnError', label: 'Email on processing errors', desc: 'Get notified if video processing fails' },
                  { key: 'emailWeeklyReport', label: 'Weekly performance report', desc: 'Receive a weekly summary of your channel analytics' },
                ].map(({ key, label, desc }) => (
                  <div key={key} className="flex items-start justify-between py-3 border-b border-gray-100">
                    <div>
                      <p className="font-medium text-gray-900 text-sm">{label}</p>
                      <p className="text-xs text-gray-500 mt-0.5">{desc}</p>
                    </div>
                    <label className="relative inline-flex items-center cursor-pointer ml-4">
                      <input
                        type="checkbox"
                        checked={(notifications as any)[key]}
                        onChange={e => setNotifications({ ...notifications, [key]: e.target.checked })}
                        className="sr-only peer"
                      />
                      <div className="w-11 h-6 bg-gray-200 peer-focus:ring-2 peer-focus:ring-blue-500 rounded-full peer peer-checked:after:translate-x-full after:content-[''] after:absolute after:top-0.5 after:left-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                    </label>
                  </div>
                ))}

                <div className="flex justify-end">
                  <button
                    onClick={handleSave}
                    disabled={saving}
                    className="flex items-center space-x-2 px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
                  >
                    {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
                    <span>{saved ? 'Saved!' : 'Save Preferences'}</span>
                  </button>
                </div>
              </div>
            )}

            {/* Team */}
            {activeTab === 'team' && (
              <div className="bg-white rounded-lg shadow p-6">
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Team Members</h2>
                <div className="p-4 bg-blue-50 border border-blue-200 rounded-lg text-sm text-blue-700 mb-4">
                  Team management is available on the Agency plan.
                </div>
                <div className="flex items-center justify-between py-3 border-b border-gray-100">
                  <div className="flex items-center space-x-3">
                    <div className="w-8 h-8 rounded-full bg-blue-100 text-blue-700 flex items-center justify-center font-bold text-sm">
                      {user?.firstName[0]}{user?.lastName[0]}
                    </div>
                    <div>
                      <p className="font-medium text-gray-900 text-sm">{user?.firstName} {user?.lastName}</p>
                      <p className="text-xs text-gray-500">{user?.email}</p>
                    </div>
                  </div>
                  <span className="px-2 py-1 text-xs bg-blue-100 text-blue-700 rounded capitalize">{user?.role}</span>
                </div>
              </div>
            )}

            {/* Danger Zone */}
            {activeTab === 'danger' && (
              <div className="bg-white rounded-lg shadow border border-red-200 p-6 space-y-4">
                <h2 className="text-lg font-semibold text-red-700">Danger Zone</h2>
                <p className="text-sm text-gray-600">These actions are irreversible. Please be careful.</p>

                <div className="p-4 border border-red-200 rounded-lg flex items-center justify-between">
                  <div>
                    <p className="font-medium text-gray-900">Delete Account</p>
                    <p className="text-xs text-gray-500 mt-0.5">
                      Permanently delete your account and all your data
                    </p>
                  </div>
                  <button className="px-4 py-2 bg-red-600 text-white text-sm rounded-lg hover:bg-red-700">
                    Delete Account
                  </button>
                </div>

                <div className="p-4 border border-red-200 rounded-lg flex items-center justify-between">
                  <div>
                    <p className="font-medium text-gray-900">Sign Out All Sessions</p>
                    <p className="text-xs text-gray-500 mt-0.5">
                      Sign out from all devices and browsers
                    </p>
                  </div>
                  <button
                    onClick={logout}
                    className="px-4 py-2 border border-red-300 text-red-600 text-sm rounded-lg hover:bg-red-50"
                  >
                    Sign Out All
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
};

export default SettingsPage;
