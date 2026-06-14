import React from 'react';
import { Loader2 } from 'lucide-react';
import { VideoJobStatus, VideoJobStatusLabels } from '../../types';

// ─── Badge ────────────────────────────────────────────────────────────────────

interface BadgeProps {
  variant?: 'default' | 'success' | 'warning' | 'error' | 'info';
  children: React.ReactNode;
  className?: string;
}

const BADGE_STYLES: Record<string, string> = {
  default: 'bg-gray-100 text-gray-700',
  success: 'bg-emerald-100 text-emerald-700',
  warning: 'bg-yellow-100 text-yellow-700',
  error: 'bg-red-100 text-red-700',
  info: 'bg-blue-100 text-blue-700',
};

export const Badge: React.FC<BadgeProps> = ({ variant = 'default', children, className = '' }) => (
  <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${BADGE_STYLES[variant]} ${className}`}>
    {children}
  </span>
);

// ─── VideoStatusBadge ─────────────────────────────────────────────────────────

interface VideoStatusBadgeProps {
  status: VideoJobStatus;
}

const STATUS_VARIANT: Record<VideoJobStatus, BadgeProps['variant']> = {
  [VideoJobStatus.Pending]: 'default',
  [VideoJobStatus.Queued]: 'info',
  [VideoJobStatus.GeneratingScript]: 'info',
  [VideoJobStatus.GeneratingScenes]: 'info',
  [VideoJobStatus.SynthesizingVoice]: 'info',
  [VideoJobStatus.FetchingBRoll]: 'info',
  [VideoJobStatus.RenderingVideo]: 'info',
  [VideoJobStatus.GeneratingThumbnail]: 'info',
  [VideoJobStatus.Uploading]: 'warning',
  [VideoJobStatus.Completed]: 'success',
  [VideoJobStatus.Failed]: 'error',
  [VideoJobStatus.Cancelled]: 'default',
};

export const VideoStatusBadge: React.FC<VideoStatusBadgeProps> = ({ status }) => {
  const isActive = status > 0 && status < VideoJobStatus.Completed;
  return (
    <Badge variant={STATUS_VARIANT[status]}>
      {isActive && <Loader2 className="w-3 h-3 mr-1 animate-spin" />}
      {VideoJobStatusLabels[status]}
    </Badge>
  );
};

// ─── Card ─────────────────────────────────────────────────────────────────────

interface CardProps {
  children: React.ReactNode;
  className?: string;
  title?: string;
  description?: string;
  actions?: React.ReactNode;
}

export const Card: React.FC<CardProps> = ({ children, className = '', title, description, actions }) => (
  <div className={`bg-white rounded-lg shadow p-6 ${className}`}>
    {(title || actions) && (
      <div className="flex items-start justify-between mb-4">
        <div>
          {title && <h3 className="text-lg font-semibold text-gray-900">{title}</h3>}
          {description && <p className="text-sm text-gray-500 mt-0.5">{description}</p>}
        </div>
        {actions && <div className="ml-4 flex-shrink-0">{actions}</div>}
      </div>
    )}
    {children}
  </div>
);

// ─── Button ───────────────────────────────────────────────────────────────────

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost' | 'outline';
  size?: 'sm' | 'md' | 'lg';
  loading?: boolean;
  leftIcon?: React.ReactNode;
}

const BUTTON_STYLES: Record<string, string> = {
  primary: 'bg-blue-600 text-white hover:bg-blue-700 disabled:bg-blue-300',
  secondary: 'bg-gray-100 text-gray-900 hover:bg-gray-200 disabled:opacity-50',
  danger: 'bg-red-600 text-white hover:bg-red-700 disabled:bg-red-300',
  ghost: 'text-gray-600 hover:bg-gray-100 disabled:opacity-50',
  outline: 'border border-gray-300 text-gray-700 hover:bg-gray-50 disabled:opacity-50',
};

const BUTTON_SIZES: Record<string, string> = {
  sm: 'px-3 py-1.5 text-sm',
  md: 'px-4 py-2 text-sm',
  lg: 'px-6 py-3 text-base',
};

export const Button: React.FC<ButtonProps> = ({
  variant = 'primary',
  size = 'md',
  loading = false,
  leftIcon,
  children,
  className = '',
  disabled,
  ...props
}) => (
  <button
    disabled={disabled || loading}
    className={`inline-flex items-center justify-center space-x-2 font-medium rounded-lg transition-colors ${BUTTON_STYLES[variant]} ${BUTTON_SIZES[size]} disabled:cursor-not-allowed ${className}`}
    {...props}
  >
    {loading ? <Loader2 className="w-4 h-4 animate-spin" /> : leftIcon}
    <span>{children}</span>
  </button>
);

// ─── Input ────────────────────────────────────────────────────────────────────

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  hint?: string;
}

export const Input: React.FC<InputProps> = ({ label, error, hint, className = '', id, ...props }) => {
  const inputId = id || label?.toLowerCase().replace(/\s/g, '-');
  return (
    <div>
      {label && (
        <label htmlFor={inputId} className="block text-sm font-medium text-gray-700 mb-1">
          {label}
          {props.required && <span className="text-red-500 ml-0.5">*</span>}
        </label>
      )}
      <input
        id={inputId}
        className={`w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors ${
          error ? 'border-red-400 bg-red-50' : 'border-gray-300'
        } ${className}`}
        {...props}
      />
      {error && <p className="text-red-500 text-xs mt-1">{error}</p>}
      {hint && !error && <p className="text-gray-400 text-xs mt-1">{hint}</p>}
    </div>
  );
};

// ─── Select ───────────────────────────────────────────────────────────────────

interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  error?: string;
  options: { value: string | number; label: string }[];
}

export const Select: React.FC<SelectProps> = ({ label, error, options, className = '', id, ...props }) => {
  const selectId = id || label?.toLowerCase().replace(/\s/g, '-');
  return (
    <div>
      {label && (
        <label htmlFor={selectId} className="block text-sm font-medium text-gray-700 mb-1">
          {label}
          {props.required && <span className="text-red-500 ml-0.5">*</span>}
        </label>
      )}
      <select
        id={selectId}
        className={`w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 ${
          error ? 'border-red-400 bg-red-50' : 'border-gray-300'
        } ${className}`}
        {...props}
      >
        {options.map(opt => (
          <option key={opt.value} value={opt.value}>{opt.label}</option>
        ))}
      </select>
      {error && <p className="text-red-500 text-xs mt-1">{error}</p>}
    </div>
  );
};

// ─── Spinner ──────────────────────────────────────────────────────────────────

interface SpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

const SPINNER_SIZES: Record<string, string> = {
  sm: 'w-4 h-4',
  md: 'w-6 h-6',
  lg: 'w-10 h-10',
};

export const Spinner: React.FC<SpinnerProps> = ({ size = 'md', className = '' }) => (
  <Loader2 className={`animate-spin text-blue-600 ${SPINNER_SIZES[size]} ${className}`} />
);

// ─── Empty State ──────────────────────────────────────────────────────────────

interface EmptyStateProps {
  icon?: React.ReactNode;
  title: string;
  description?: string;
  action?: React.ReactNode;
}

export const EmptyState: React.FC<EmptyStateProps> = ({ icon, title, description, action }) => (
  <div className="flex flex-col items-center justify-center py-16 px-4 text-center">
    {icon && <div className="text-gray-300 mb-4">{icon}</div>}
    <h3 className="text-lg font-medium text-gray-900 mb-2">{title}</h3>
    {description && <p className="text-sm text-gray-500 mb-6 max-w-md">{description}</p>}
    {action}
  </div>
);

// ─── Alert ────────────────────────────────────────────────────────────────────

interface AlertProps {
  variant?: 'info' | 'success' | 'warning' | 'error';
  title?: string;
  children: React.ReactNode;
}

const ALERT_STYLES: Record<string, string> = {
  info: 'bg-blue-50 border-blue-200 text-blue-800',
  success: 'bg-emerald-50 border-emerald-200 text-emerald-800',
  warning: 'bg-yellow-50 border-yellow-200 text-yellow-800',
  error: 'bg-red-50 border-red-200 text-red-800',
};

export const Alert: React.FC<AlertProps> = ({ variant = 'info', title, children }) => (
  <div className={`border rounded-lg p-4 ${ALERT_STYLES[variant]}`}>
    {title && <p className="font-semibold mb-1">{title}</p>}
    <div className="text-sm">{children}</div>
  </div>
);

// ─── ProgressBar ─────────────────────────────────────────────────────────────

interface ProgressBarProps {
  value: number;        // 0–100
  label?: string;
  color?: 'blue' | 'green' | 'red' | 'yellow';
  showPercent?: boolean;
}

const PROGRESS_COLORS: Record<string, string> = {
  blue: 'bg-blue-600',
  green: 'bg-emerald-500',
  red: 'bg-red-500',
  yellow: 'bg-yellow-500',
};

export const ProgressBar: React.FC<ProgressBarProps> = ({
  value,
  label,
  color = 'blue',
  showPercent = false,
}) => (
  <div>
    {(label || showPercent) && (
      <div className="flex justify-between mb-1">
        {label && <span className="text-sm font-medium text-gray-700">{label}</span>}
        {showPercent && <span className="text-sm text-gray-500">{Math.round(value)}%</span>}
      </div>
    )}
    <div className="w-full bg-gray-200 rounded-full h-2.5">
      <div
        className={`h-2.5 rounded-full transition-all duration-500 ${PROGRESS_COLORS[color]}`}
        style={{ width: `${Math.min(Math.max(value, 0), 100)}%` }}
      />
    </div>
  </div>
);

// ─── StatCard ─────────────────────────────────────────────────────────────────

interface StatCardProps {
  label: string;
  value: string | number;
  icon?: React.ReactNode;
  trend?: { value: number; label: string };
  color?: 'blue' | 'green' | 'purple' | 'orange';
}

const STAT_COLORS: Record<string, string> = {
  blue: 'bg-blue-50 text-blue-600',
  green: 'bg-emerald-50 text-emerald-600',
  purple: 'bg-purple-50 text-purple-600',
  orange: 'bg-orange-50 text-orange-600',
};

export const StatCard: React.FC<StatCardProps> = ({ label, value, icon, trend, color = 'blue' }) => (
  <div className="bg-white rounded-lg shadow p-5">
    <div className="flex items-center justify-between mb-3">
      <p className="text-sm font-medium text-gray-500">{label}</p>
      {icon && (
        <div className={`w-9 h-9 rounded-lg flex items-center justify-center ${STAT_COLORS[color]}`}>
          {icon}
        </div>
      )}
    </div>
    <p className="text-2xl font-bold text-gray-900">{value}</p>
    {trend && (
      <p className={`text-xs mt-1 ${trend.value >= 0 ? 'text-emerald-600' : 'text-red-600'}`}>
        {trend.value >= 0 ? '↑' : '↓'} {Math.abs(trend.value)}% {trend.label}
      </p>
    )}
  </div>
);

// ─── Modal ────────────────────────────────────────────────────────────────────

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  actions?: React.ReactNode;
  size?: 'sm' | 'md' | 'lg';
}

const MODAL_SIZES: Record<string, string> = {
  sm: 'max-w-sm',
  md: 'max-w-md',
  lg: 'max-w-2xl',
};

export const Modal: React.FC<ModalProps> = ({ isOpen, onClose, title, children, actions, size = 'md' }) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black bg-opacity-40" onClick={onClose} />

      {/* Modal */}
      <div className={`relative bg-white rounded-xl shadow-xl w-full ${MODAL_SIZES[size]}`}>
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <h2 className="text-lg font-semibold text-gray-900">{title}</h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
          >
            ✕
          </button>
        </div>

        {/* Body */}
        <div className="px-6 py-4">{children}</div>

        {/* Footer */}
        {actions && (
          <div className="flex justify-end space-x-3 px-6 py-4 border-t border-gray-100">
            {actions}
          </div>
        )}
      </div>
    </div>
  );
};
