import { useQuery, useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { apiService } from '../services/apiClient';
import { useAuthStore } from '../store/authStore';
import { ArrowLeft, Check, CreditCard, Loader2, ExternalLink } from 'lucide-react';

const PLANS = [
  {
    id: 'starter',
    name: 'Starter',
    price: 19,
    videos: 5,
    features: [
      '5 AI videos per month',
      'Standard quality scripts',
      'ElevenLabs voice synthesis',
      'Pexels B-Roll footage',
      'YouTube auto-upload',
      'Basic analytics',
    ],
  },
  {
    id: 'pro',
    name: 'Pro',
    price: 49,
    videos: 25,
    popular: true,
    features: [
      '25 AI videos per month',
      'Advanced GPT-4 scripts',
      'Premium voice options',
      'HD B-Roll footage',
      'YouTube auto-upload',
      'Advanced analytics',
      'A/B testing variants',
      'Custom thumbnails',
    ],
  },
  {
    id: 'agency',
    name: 'Agency',
    price: 149,
    videos: -1,
    features: [
      'Unlimited AI videos',
      'Claude/GPT-4 scripts',
      'All voice options',
      '4K B-Roll footage',
      'Multi-channel upload',
      'Full analytics suite',
      'Unlimited variants',
      'Priority processing',
      'API access',
      'Team members',
    ],
  },
];

const BillingPage = () => {
  const navigate = useNavigate();
  const { tenant } = useAuthStore();

  const { data: subscription, isLoading } = useQuery({
    queryKey: ['subscription'],
    queryFn: () => apiService.getSubscription().then(r => r.data),
  });

  const checkoutMutation = useMutation({
    mutationFn: (priceId: string) => apiService.getCheckoutUrl(priceId),
    onSuccess: (res) => {
      window.location.href = res.data.url;
    },
  });

  const portalMutation = useMutation({
    mutationFn: () => apiService.getBillingPortal(),
    onSuccess: (res) => {
      window.location.href = res.data.url;
    },
  });

  const getPriceId = (planId: string) => {
    const priceIds: Record<string, string> = {
      starter: import.meta.env.VITE_STRIPE_PRICE_STARTER || 'price_starter',
      pro: import.meta.env.VITE_STRIPE_PRICE_PRO || 'price_pro',
      agency: import.meta.env.VITE_STRIPE_PRICE_AGENCY || 'price_agency',
    };
    return priceIds[planId];
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-5xl mx-auto px-4 py-4 flex items-center space-x-4">
          <button onClick={() => navigate('/dashboard')} className="text-gray-500 hover:text-gray-700">
            <ArrowLeft className="w-5 h-5" />
          </button>
          <div>
            <h1 className="text-xl font-bold text-gray-900">Billing & Plans</h1>
            <p className="text-sm text-gray-500">Manage your subscription</p>
          </div>
        </div>
      </header>

      <main className="max-w-5xl mx-auto px-4 py-8 space-y-8">

        {/* Current Plan Summary */}
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-lg font-semibold text-gray-900">Current Plan</h2>
              {isLoading ? (
                <p className="text-gray-400 mt-1">Loading...</p>
              ) : (
                <>
                  <p className="text-gray-600 mt-1">
                    <span className="font-medium text-blue-600 capitalize">{tenant?.plan}</span> plan •{' '}
                    {tenant?.videosCreatedThisMonth} of {tenant?.monthlyVideoQuota} videos used this month
                  </p>
                  {subscription?.currentPeriodEnd && (
                    <p className="text-xs text-gray-400 mt-1">
                      Next billing: {new Date(subscription.currentPeriodEnd).toLocaleDateString()}
                    </p>
                  )}
                </>
              )}
            </div>
            {subscription && (
              <button
                onClick={() => portalMutation.mutate()}
                disabled={portalMutation.isPending}
                className="flex items-center space-x-2 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 text-sm"
              >
                {portalMutation.isPending ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <CreditCard className="w-4 h-4" />
                )}
                <span>Manage Billing</span>
                <ExternalLink className="w-3 h-3" />
              </button>
            )}
          </div>

          {/* Quota bar */}
          <div className="mt-4">
            <div className="flex justify-between text-xs text-gray-500 mb-1">
              <span>Videos used this month</span>
              <span>{tenant?.videosCreatedThisMonth} / {tenant?.monthlyVideoQuota}</span>
            </div>
            <div className="w-full bg-gray-200 rounded-full h-2">
              <div
                className={`h-2 rounded-full transition-all ${
                  ((tenant?.videosCreatedThisMonth || 0) / (tenant?.monthlyVideoQuota || 1)) > 0.8
                    ? 'bg-red-500'
                    : 'bg-blue-500'
                }`}
                style={{
                  width: `${Math.min(
                    ((tenant?.videosCreatedThisMonth || 0) / (tenant?.monthlyVideoQuota || 1)) * 100,
                    100
                  )}%`,
                }}
              />
            </div>
          </div>
        </div>

        {/* Plans */}
        <div>
          <h2 className="text-xl font-bold text-gray-900 mb-2">Choose Your Plan</h2>
          <p className="text-gray-500 mb-6">Upgrade or downgrade at any time. Billed monthly.</p>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {PLANS.map((plan) => {
              const isCurrent = tenant?.plan === plan.id;
              return (
                <div
                  key={plan.id}
                  className={`bg-white rounded-xl shadow p-6 relative ${
                    plan.popular ? 'ring-2 ring-blue-500' : ''
                  }`}
                >
                  {plan.popular && (
                    <span className="absolute -top-3 left-1/2 -translate-x-1/2 px-4 py-1 bg-blue-600 text-white text-xs font-bold rounded-full">
                      MOST POPULAR
                    </span>
                  )}

                  <div className="mb-4">
                    <h3 className="text-xl font-bold text-gray-900">{plan.name}</h3>
                    <div className="mt-2">
                      <span className="text-4xl font-extrabold text-gray-900">${plan.price}</span>
                      <span className="text-gray-500 text-sm">/month</span>
                    </div>
                    <p className="text-sm text-blue-600 font-medium mt-1">
                      {plan.videos === -1 ? 'Unlimited' : `${plan.videos}`} videos/month
                    </p>
                  </div>

                  <ul className="space-y-2 mb-6">
                    {plan.features.map((feature) => (
                      <li key={feature} className="flex items-start space-x-2 text-sm text-gray-600">
                        <Check className="w-4 h-4 text-green-500 mt-0.5 flex-shrink-0" />
                        <span>{feature}</span>
                      </li>
                    ))}
                  </ul>

                  {isCurrent ? (
                    <div className="w-full py-2 px-4 bg-gray-100 text-gray-500 font-medium rounded-lg text-center text-sm">
                      ✓ Current Plan
                    </div>
                  ) : (
                    <button
                      onClick={() => checkoutMutation.mutate(getPriceId(plan.id))}
                      disabled={checkoutMutation.isPending}
                      className={`w-full py-2 px-4 font-medium rounded-lg transition-colors text-sm ${
                        plan.popular
                          ? 'bg-blue-600 text-white hover:bg-blue-700'
                          : 'border border-blue-300 text-blue-600 hover:bg-blue-50'
                      } disabled:opacity-50 disabled:cursor-not-allowed`}
                    >
                      {checkoutMutation.isPending ? (
                        <Loader2 className="w-4 h-4 animate-spin mx-auto" />
                      ) : (
                        `Upgrade to ${plan.name}`
                      )}
                    </button>
                  )}
                </div>
              );
            })}
          </div>
        </div>

        {/* FAQ */}
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Frequently Asked Questions</h2>
          <div className="space-y-4">
            {[
              {
                q: 'Can I cancel at any time?',
                a: 'Yes, you can cancel your subscription at any time. Your access continues until the end of the current billing period.',
              },
              {
                q: 'What happens if I exceed my monthly video quota?',
                a: 'Video creation will be paused until your quota resets at the start of the next billing cycle, or you upgrade your plan.',
              },
              {
                q: 'Do unused videos roll over?',
                a: 'No, unused videos do not roll over to the next month. Your quota resets on your billing date.',
              },
              {
                q: 'Is there a free trial?',
                a: 'Yes! All new accounts start with a 14-day free trial on the Pro plan. No credit card required.',
              },
            ].map(({ q, a }) => (
              <div key={q} className="border-b border-gray-100 pb-4 last:border-0">
                <p className="font-medium text-gray-900 mb-1">{q}</p>
                <p className="text-sm text-gray-600">{a}</p>
              </div>
            ))}
          </div>
        </div>
      </main>
    </div>
  );
};

export default BillingPage;
