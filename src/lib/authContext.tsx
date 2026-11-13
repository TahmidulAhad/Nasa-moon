import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import {
  changePassword as apiChangePassword,
  fetchMe,
  getCurrentUser,
  login as apiLogin,
  logout as apiLogout,
  register as apiRegister,
} from './auth';
import type { User } from './auth';

interface AuthContextValue {
  /** The logged-in researcher, or null when logged out. */
  user: User | null;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, displayName: string) => Promise<void>;
  logout: () => Promise<void>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
  /** Re-read /api/auth/me (e.g. after a page reloads with a stored token). */
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/**
 * Auth state for the whole app. The user is restored from localStorage on
 * first render (token + stored profile); refreshUser re-reads the profile
 * from the API when a page needs it fresh.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => getCurrentUser());

  const login = useCallback(async (email: string, password: string) => {
    const auth = await apiLogin(email, password);
    setUser(auth.user);
  }, []);

  const register = useCallback(
    async (email: string, password: string, displayName: string) => {
      const auth = await apiRegister(email, password, displayName);
      setUser(auth.user);
    },
    [],
  );

  const logout = useCallback(async () => {
    await apiLogout();
    setUser(null);
  }, []);

  const changePassword = useCallback(
    async (currentPassword: string, newPassword: string) => {
      await apiChangePassword(currentPassword, newPassword);
    },
    [],
  );

  const refreshUser = useCallback(async () => {
    try {
      setUser(await fetchMe());
    } catch {
      setUser(null);
    }
  }, []);

  const value = useMemo(
    () => ({ user, login, register, logout, changePassword, refreshUser }),
    [user, login, register, logout, changePassword, refreshUser],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>.');
  return ctx;
}
