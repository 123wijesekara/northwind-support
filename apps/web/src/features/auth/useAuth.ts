import { useEffect, useState } from 'react';
import { getUser, isAuthenticated, signIn, signOut, subscribe } from './authStore';
import type { AuthenticatedUser } from '../../types/api';

/** React hook that returns the current auth state and the actions to change it. */
export function useAuth() {
  const [user, setUser] = useState<AuthenticatedUser | null>(() => getUser());
  const [authenticated, setAuthenticated] = useState<boolean>(() => isAuthenticated());

  useEffect(() => {
    const unsubscribe = subscribe(() => {
      setUser(getUser());
      setAuthenticated(isAuthenticated());
    });
    return unsubscribe;
  }, []);

  return { user, isAuthenticated: authenticated, signIn, signOut };
}