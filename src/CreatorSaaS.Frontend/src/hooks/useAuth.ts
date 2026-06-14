import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { apiService } from '../services/apiClient';

export const useAuth = () => {
  const navigate = useNavigate();
  const store = useAuthStore();

  const loginWithCredentials = useCallback(async (email: string, password: string) => {
    const res = await apiService.login(email, password);
    const { accessToken, refreshToken, user, tenant } = res.data;
    store.setTokens(accessToken, refreshToken);
    store.setUser(user);
    return { user, tenant };
  }, [store]);

  const logout = useCallback(() => {
    store.logout();
    navigate('/login');
  }, [store, navigate]);

  const isOwner = store.user?.role === 'owner';
  const isAdmin = store.user?.role === 'admin' || isOwner;

  return {
    user: store.user,
    tenant: store.tenant,
    accessToken: store.accessToken,
    isAuthenticated: !!store.accessToken,
    isLoading: store.isLoading,
    isOwner,
    isAdmin,
    loginWithCredentials,
    logout,
  };
};
