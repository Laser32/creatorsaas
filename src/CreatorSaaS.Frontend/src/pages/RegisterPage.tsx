import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { Check } from 'lucide-react';

const PLANS = [
  { id: 'starter', name: 'Starter', price: 19, videos: 5, description: 'Perfect for getting started' },
  { id: 'pro', name: 'Pro', price: 49, videos: 25, description: 'For growing channels', popular: true },
  { id: 'agency', name: 'Agency', price: 149, videos: 999, description: 'Unlimited video creation' },
];

const RegisterPage = () => {
  const navigate = useNavigate();
  const { register, isLoading } = useAuthStore();

  const [step, setStep] = useState<1 | 2>(1);
  const [selectedPlan, setSelectedPlan] = useState('pro');
  const [form, setForm] = useState({
    tenantName: '',
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
  });
  const [error, setError] = useState<string | null>(null);

  const handleStep1 = (e: React.FormEvent) => {
    e.preventDefault();
    if (form.password !== form.confirmPassword) {
      setError('Passwords do not match');
      return;
    }
    if (form.password.length < 8) {
      setError('Password must be at least 8 characters');
      return;
    }
    setError(null);
    setStep(2);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await register(form.tenantName, form.email, form.password, form.firstName, form.lastName);
      navigate('/dashboard');
    } catch (err: any) {
      setError(err.message || 'Registration failed. Please try again.');
    }
  };

  return (
    <div className="min-h-screen flex bg-gradient-to-br from-blue-50 to-indigo-100">
      {/* Left: Plans */}
      <div className="hidden lg:flex lg:w-1/2 flex-col justify-center px-16 py-12">
        <h1 className="text-4xl font-bold text-gray-900 mb-2">CreatorSaaS</h1>
        <p className="text-lg text-gray-600 mb-10">
          Automate your YouTube channel with AI-generated videos
        </p>

        <div className="space-y-4">
          {PLANS.map(plan => (
            <button
              key={plan.id}
              type="button"
              onClick={() => setSelectedPlan(plan.id)}
              className={`w-full text-left p-5 rounded-xl border-2 transition-all ${
                selectedPlan === plan.id
                  ? 'border-blue-500 bg-blue-50'
                  : 'border-gray-200 bg-white hover:border-blue-300'
              }`}
            >
              <div className="flex items-center justify-between mb-1">
                <div className="flex items-center space-x-2">
                  <span className="font-bold text-gray-900 text-lg">{plan.name}</span>
                  {plan.popular && (
                    <span className="px-2 py-0.5 text-xs bg-blue-600 text-white rounded-full">Popular</span>
                  )}
                </div>
                <span className="text-2xl font-bold text-gray-900">
                  ${plan.price}<span className="text-sm font-normal text-gray-500">/mo</span>
                </span>
              </div>
              <p className="text-sm text-gray-500 mb-2">{plan.description}</p>
              <div className="flex items-center space-x-1 text-sm text-blue-600">
                <Check className="w-4 h-4" />
                <span>{plan.videos === 999 ? 'Unlimited' : plan.videos} videos/month</span>
              </div>
            </button>
          ))}
        </div>

        <div className="mt-8 p-4 bg-white rounded-xl border border-gray-200">
          <p className="text-sm font-medium text-gray-700 mb-3">All plans include:</p>
          <div className="space-y-2">
            {[
              'AI Script Generation (OpenAI/Anthropic)',
              'Professional Voice Synthesis (ElevenLabs)',
              'Automatic B-Roll Footage (Pexels)',
              'AI Thumbnail Generator',
              'YouTube Auto-Upload',
              'Analytics Dashboard',
            ].map(feature => (
              <div key={feature} className="flex items-center space-x-2 text-sm text-gray-600">
                <Check className="w-4 h-4 text-green-500 flex-shrink-0" />
                <span>{feature}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Right: Form */}
      <div className="w-full lg:w-1/2 flex flex-col justify-center px-8 lg:px-16 py-12">
        <div className="max-w-md w-full mx-auto">
          {/* Step indicator */}
          <div className="flex items-center space-x-3 mb-8">
            {[1, 2].map(s => (
              <div key={s} className="flex items-center space-x-2">
                <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold ${
                  step >= s ? 'bg-blue-600 text-white' : 'bg-gray-200 text-gray-500'
                }`}>
                  {step > s ? <Check className="w-4 h-4" /> : s}
                </div>
                <span className={`text-sm ${step >= s ? 'text-gray-900 font-medium' : 'text-gray-400'}`}>
                  {s === 1 ? 'Account Info' : 'Company Details'}
                </span>
                {s < 2 && <span className="text-gray-300 mx-2">→</span>}
              </div>
            ))}
          </div>

          <h2 className="text-2xl font-bold text-gray-900 mb-6">
            {step === 1 ? 'Create Your Account' : 'Set Up Your Company'}
          </h2>

          {error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
              {error}
            </div>
          )}

          {step === 1 ? (
            <form onSubmit={handleStep1} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">First Name</label>
                  <input
                    type="text"
                    value={form.firstName}
                    onChange={e => setForm({ ...form, firstName: e.target.value })}
                    placeholder="John"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
                  <input
                    type="text"
                    value={form.lastName}
                    onChange={e => setForm({ ...form, lastName: e.target.value })}
                    placeholder="Doe"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                    required
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Email Address</label>
                <input
                  type="email"
                  value={form.email}
                  onChange={e => setForm({ ...form, email: e.target.value })}
                  placeholder="john@example.com"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Password</label>
                <input
                  type="password"
                  value={form.password}
                  onChange={e => setForm({ ...form, password: e.target.value })}
                  placeholder="Min. 8 characters"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  required
                  minLength={8}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Confirm Password</label>
                <input
                  type="password"
                  value={form.confirmPassword}
                  onChange={e => setForm({ ...form, confirmPassword: e.target.value })}
                  placeholder="Repeat password"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  required
                />
              </div>

              <button
                type="submit"
                className="w-full py-2 px-4 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors"
              >
                Continue →
              </button>
            </form>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Company / Agency Name
                </label>
                <input
                  type="text"
                  value={form.tenantName}
                  onChange={e => setForm({ ...form, tenantName: e.target.value })}
                  placeholder="My Video Agency"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  required
                />
                <p className="text-xs text-gray-400 mt-1">
                  This creates your workspace where you can manage projects and channels
                </p>
              </div>

              <div className="p-4 bg-blue-50 border border-blue-200 rounded-lg">
                <p className="text-sm font-medium text-blue-800 mb-1">
                  Selected Plan: {PLANS.find(p => p.id === selectedPlan)?.name}
                </p>
                <p className="text-sm text-blue-600">
                  ${PLANS.find(p => p.id === selectedPlan)?.price}/month •{' '}
                  {PLANS.find(p => p.id === selectedPlan)?.videos === 999
                    ? 'Unlimited'
                    : PLANS.find(p => p.id === selectedPlan)?.videos} videos/month
                </p>
                <button
                  type="button"
                  onClick={() => setStep(1)}
                  className="text-xs text-blue-500 hover:underline mt-1"
                >
                  Change plan
                </button>
              </div>

              <div className="flex items-start space-x-2">
                <input type="checkbox" id="terms" required className="mt-1 w-4 h-4 text-blue-600 rounded" />
                <label htmlFor="terms" className="text-sm text-gray-600">
                  I agree to the{' '}
                  <a href="#" className="text-blue-600 hover:underline">Terms of Service</a>{' '}
                  and{' '}
                  <a href="#" className="text-blue-600 hover:underline">Privacy Policy</a>
                </label>
              </div>

              <div className="flex space-x-3">
                <button
                  type="button"
                  onClick={() => setStep(1)}
                  className="flex-1 py-2 px-4 border border-gray-300 text-gray-700 font-medium rounded-lg hover:bg-gray-50"
                >
                  ← Back
                </button>
                <button
                  type="submit"
                  disabled={isLoading}
                  className="flex-1 py-2 px-4 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
                >
                  {isLoading ? 'Creating account...' : 'Start Free Trial'}
                </button>
              </div>
            </form>
          )}

          <div className="mt-6 text-center text-sm text-gray-600">
            Already have an account?{' '}
            <Link to="/login" className="text-blue-600 hover:text-blue-700 font-medium">
              Sign in
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

export default RegisterPage;
