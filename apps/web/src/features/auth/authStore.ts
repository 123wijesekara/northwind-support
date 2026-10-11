import type { AuthenticatedUser } from '../../types/api';
import { clearToken, getToken, setToken } from '../../api/client';

const USER_KEY = 'supportdesk.user';

let currentUser: AuthenticatedUser | null = readStoredUser();
const listeners = new Set<() => void>();

function readStoredUser(): AuthenticatedUser | null {
  const raw = sessionStorage.getItem(USER_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as AuthenticatedUser;
  } catch {
    return null;
  }
}

function writeStoredUser(user: AuthenticatedUser | null): void {
  if (user) {
    sessionStorage.setItem(USER_KEY, JSON.stringify(user));
  } else {
    sessionStorage.removeItem(USER_KEY);
  }
}

function notify(): void {
  for (const listener of listeners) {
    listener();
  }
}

/** Subscribe to user changes. Returns an unsubscribe function. */
export function subscribe(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

/** The current signed-in user, or null. */
export function getUser(): AuthenticatedUser | null {
  return currentUser;
}

/** Whether someone is signed in. */
export function isAuthenticated(): boolean {
  return currentUser !== null && getToken() !== null;
}

/** Store a successful sign-in. */
export function signIn(token: string, user: AuthenticatedUser): void {
  setToken(token);
  currentUser = user;
  writeStoredUser(user);
  notify();
}

/** Clear the session. Called on logout and on any 401. */
export function signOut(): void {
  clearToken();
  currentUser = null;
  writeStoredUser(null);
  notify();
}