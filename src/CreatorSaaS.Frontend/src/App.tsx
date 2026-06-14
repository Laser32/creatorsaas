import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useAuthStore } from './store/authStore';

// Pages
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import VideoCreatePage from './pages/VideoCreatePage';
import VideoDetailPage from './pages/VideoDetailPage';
import SettingsPage from './pages/SettingsPage';
import BillingPage from './pages/BillingPage';

const queryClient = new QueryClient();

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const { accessToken } = useAuthStore();
  return accessToken ? <>{children}</> : <Navigate to="/login" />;
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <Router>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          <Route
            path="/dashboard"
            element={
              <PrivateRoute>
                <DashboardPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/videos/new"
            element={
              <PrivateRoute>
                <VideoCreatePage />
              </PrivateRoute>
            }
          />
          <Route
            path="/videos/:id"
            element={
              <PrivateRoute>
                <VideoDetailPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/settings"
            element={
              <PrivateRoute>
                <SettingsPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/billing"
            element={
              <PrivateRoute>
                <BillingPage />
              </PrivateRoute>
            }
          />

          <Route path="/" element={<Navigate to="/dashboard" />} />
        </Routes>
      </Router>
    </QueryClientProvider>
  );
}

export default App;
