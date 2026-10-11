import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';
import { router } from './routes';
import { signOut } from './features/auth/authStore';
import './index.css';

// Redirect to login whenever the API answers 401 (expired token, revoked session).
window.addEventListener('supportdesk:unauthorized', () => {
  signOut();
  void router.navigate('/login');
});

const container = document.getElementById('root');

if (!container) {
  throw new Error('Root element is missing from index.html.');
}

createRoot(container).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
);