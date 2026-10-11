import { request } from './client';
import type { AuthenticatedUser, LoginResponse } from '../types/api';

export const authApi = {
  login: (email: string, password: string) =>
    request<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),

  me: () => request<AuthenticatedUser>('/auth/me'),
};