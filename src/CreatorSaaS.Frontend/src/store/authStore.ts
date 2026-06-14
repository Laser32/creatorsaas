import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'owner' | 'admin' | 'member';
  tenantId: string;
  avatarUrl?: string;
}

export interface Tenant {
  id: string;
  name: string;
  slug: string;
  plan: 'starter' | 'pro' | 'agency';
  monthlyVideoQuota: number;
  videosCreatedThisMonth: number;
}

interface AuthState {
  user: User | null;
  tenant: Tenant | null;
  accessToken: string | null;
  refreshToken: string | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (tenantName: string, email: string, password: string, firstName: string, lastName: string) => Promise<void>;
  logout: () => void;
  setTokens: (accessToken: string, refreshToken: string) => void;
  setUser: (user: User) => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      tenant: null,
      accessToken: null,
      refreshToken: null,
      isLoading: false,

      login: async (email: string, password: string) => {
        set({ isLoading: true });
        try {
          const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password }),
          });
          const data = await response.json();
          set({
            accessToken: data.accessToken,
            refreshToken: data.refreshToken,
            user: data.user,
            tenant: data.tenant,
          });
        } finally {
          set({ isLoading: false });
        }
      },

      register: async (tenantName: string, email: string, password: string, firstName: string, lastName: string) => {
        set({ isLoading: true });
        try {
          const response = await fetch('/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ tenantName, email, password, firstName, lastName }),
          });
          const data = await response.json();
          set({
            accessToken: data.accessToken,
            refreshToken: data.refreshToken,
            user: data.user,
            tenant: data.tenant,
          });
        } finally {
          set({ isLoading: false });
        }
      },

      logout: () => {
        set({
          user: null,
          tenant: null,
          accessToken: null,
          refreshToken: null,
        });
      },

      setTokens: (accessToken: string, refreshToken: string) => {
        set({ accessToken, refreshToken });
      },

      setUser: (user: User) => {
        set({ user });
      },
    }),
    {
      name: 'auth-store',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        user: state.user,
        tenant: state.tenant,
      }),
    }
  )
);
