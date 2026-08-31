import { create } from 'zustand';
import type { AuthResponse, Branch, User } from '@/types';

interface AuthState {
  user: User | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  login: (response: AuthResponse) => void;
  logout: () => void;
  loadFromStorage: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: null,
  isAuthenticated: false,

  login: (response: AuthResponse) => {
    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);
    localStorage.setItem('user', JSON.stringify(response.user));
    set({
      user: response.user,
      accessToken: response.accessToken,
      isAuthenticated: true,
    });
  },

  logout: () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    set({ user: null, accessToken: null, isAuthenticated: false });
  },

  loadFromStorage: () => {
    const token = localStorage.getItem('accessToken');
    const userData = localStorage.getItem('user');
    if (token && userData) {
      set({
        accessToken: token,
        user: JSON.parse(userData),
        isAuthenticated: true,
      });
    }
  },
}));

interface BranchState {
  branches: Branch[];
  selectedBranchId: string | null;
  setBranches: (branches: Branch[]) => void;
  selectBranch: (branchId: string) => void;
}

export const useBranchStore = create<BranchState>((set) => ({
  branches: [],
  selectedBranchId: localStorage.getItem('selectedBranchId'),

  setBranches: (branches: Branch[]) => set({ branches }),

  selectBranch: (branchId: string) => {
    localStorage.setItem('selectedBranchId', branchId);
    set({ selectedBranchId: branchId });
  },
}));
