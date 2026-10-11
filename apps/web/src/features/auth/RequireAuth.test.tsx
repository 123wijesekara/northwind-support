import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { RequireAuth } from './RequireAuth';
import * as store from './authStore';

describe('RequireAuth', () => {
  afterEach(() => vi.restoreAllMocks());

  it('redirects to /login when not signed in', () => {
    vi.spyOn(store, 'isAuthenticated').mockReturnValue(false);

    render(
      <MemoryRouter initialEntries={['/tickets']}>
        <Routes>
          <Route element={<RequireAuth />}>
            <Route path="/tickets" element={<div>Secret</div>} />
          </Route>
          <Route path="/login" element={<div>Login page</div>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('Login page')).toBeTruthy();
    expect(screen.queryByText('Secret')).toBeNull();
  });

  it('renders the child route when signed in', () => {
    vi.spyOn(store, 'isAuthenticated').mockReturnValue(true);

    render(
      <MemoryRouter initialEntries={['/tickets']}>
        <Routes>
          <Route element={<RequireAuth />}>
            <Route path="/tickets" element={<div>Secret</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('Secret')).toBeTruthy();
  });
});