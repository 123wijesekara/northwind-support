import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { authApi } from '../../api/auth';
import { ApiError } from '../../api/client';
import { LoginPage } from './LoginPage';

vi.mock('../../api/auth', () => ({
  authApi: {
    login: vi.fn(),
  },
}));

const login = vi.mocked(authApi.login);

function renderPage() {
  render(
    <MemoryRouter>
      <LoginPage />
    </MemoryRouter>,
  );
}

describe('LoginPage', () => {
  afterEach(() => vi.clearAllMocks());

  it('blocks submission when email or password is empty', () => {
    renderPage();

    const button = screen.getByRole('button', { name: /Sign in/ }) as HTMLButtonElement;
    expect(button.disabled).toBe(true);

    fireEvent.change(screen.getByLabelText(/Email/), { target: { value: 'alex@example.com' } });
    expect(button.disabled).toBe(true);

    fireEvent.change(screen.getByLabelText(/Password/), { target: { value: 'p' } });
    expect(button.disabled).toBe(false);

    expect(login).not.toHaveBeenCalled();
  });

  it('shows the error message when the server rejects the credentials', async () => {
    login.mockRejectedValueOnce(new ApiError(401, 'Invalid email or password.'));

    renderPage();

    fireEvent.change(screen.getByLabelText(/Email/), { target: { value: 'alex@example.com' } });
    fireEvent.change(screen.getByLabelText(/Password/), { target: { value: 'wrong' } });
    fireEvent.click(screen.getByRole('button', { name: /Sign in/ }));

    await waitFor(() => {
      expect(screen.getByText('Invalid email or password.')).toBeTruthy();
    });
  });
});