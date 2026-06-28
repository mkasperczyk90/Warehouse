import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { color } from '@/shared/theme/tokens';
import { ThemeProvider } from '@/shared/theme/theme';
import { I18nProvider } from '@/shared/i18n/i18n';
import { AuthProvider, useAuth } from '@/shared/auth/AuthContext';
import { LoginScreen } from '@/features/Auth';

/**
 * Root layout — global configuration for the whole app: providers and the
 * navigation Stack. The individual route files (index, receive, …) are thin
 * re-exports of feature screens; the real work lives in `src/features`.
 *
 * The terminal always talks to the real Gateway (proxied at /api by nginx); the operator badges in to
 * get a token, which the api seam carries on every request.
 */
export default function RootLayout() {
  return (
    <SafeAreaProvider>
      <I18nProvider>
        <ThemeProvider>
          <AuthProvider>
            <StatusBar style="light" />
            <RootNavigator />
          </AuthProvider>
        </ThemeProvider>
      </I18nProvider>
    </SafeAreaProvider>
  );
}

/**
 * Auth gate: no operator signed in → the badge-scan login; otherwise the task
 * stack. The operator badges in on the handheld (the terminal has no desk
 * session), so this is the only thing standing between boot and the hub.
 */
function RootNavigator() {
  const { operator } = useAuth();

  if (!operator) return <LoginScreen />;

  return (
    <Stack
      screenOptions={{
        headerShown: false,
        contentStyle: { backgroundColor: color.canvas },
        animation: 'slide_from_right',
      }}
    >
      <Stack.Screen name="index" />
      <Stack.Screen name="receive" />
      <Stack.Screen name="putaway" />
      <Stack.Screen name="pick" />
      <Stack.Screen name="move" />
      <Stack.Screen name="pack" />
      <Stack.Screen name="scan" />
      <Stack.Screen name="lookup" />
    </Stack>
  );
}
