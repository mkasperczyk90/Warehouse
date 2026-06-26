import { RouterProvider } from '@tanstack/react-router';

import { AuthProvider, useAuth } from '@/shared/auth/AuthContext';
import { WarehouseProvider } from '@/shared/warehouse/WarehouseContext';
import { ToastProvider } from '@/shared/toast';
import { LoginScreen } from '@/features/Auth';
import { router } from './router';

/**
 * Until the desk user signs in (badge scan) there is no warehouse to scope to,
 * so we show the login screen instead of the app. Once authenticated, the
 * WarehouseProvider opens on the user's default site and the router takes over.
 */
function Gate() {
  const { user } = useAuth();
  if (!user) return <LoginScreen />;
  return (
    <WarehouseProvider initialWarehouseId={user.defaultWarehouseId}>
      <RouterProvider router={router} />
    </WarehouseProvider>
  );
}

export function App() {
  return (
    <ToastProvider>
      <AuthProvider>
        <Gate />
      </AuthProvider>
    </ToastProvider>
  );
}
