import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useEffect, useState } from 'react';
import { Platform } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { color } from '@/shared/theme/tokens';
import { ThemeProvider } from '@/shared/theme/theme';
import { I18nProvider } from '@/shared/i18n/i18n';
import { AuthProvider, useAuth } from '@/shared/auth/AuthContext';
import { LoginScreen } from '@/features/Auth';

/**
 * MSW fixtures are the default (ADR-0006) — a Service Worker on web, a request interceptor on a
 * handheld — so dev, tests and the standalone preview image keep working untouched. Build with
 * `EXPO_PUBLIC_USE_MOCKS=false` to turn the mocks off and let the same `fetch` calls hit the real
 * Gateway (proxied at /api by nginx). It is a build-time flag (Metro inlines EXPO_PUBLIC_* at export).
 * We hold the first render until the mocks are armed so no screen fetches before MSW can intercept.
 */
const USE_MOCKS = process.env.EXPO_PUBLIC_USE_MOCKS !== 'false';

async function enableMocking() {
  if (!USE_MOCKS) return;
  if (Platform.OS === 'web') {
    const { worker } = await import('@/core/mocks/browser');
    await worker.start({ onUnhandledRequest: 'bypass' });
  } else {
    const { server } = await import('@/core/mocks/native');
    server.listen({ onUnhandledRequest: 'bypass' });
  }
}

/**
 * Root layout — global configuration for the whole app: providers and the
 * navigation Stack. The individual route files (index, receive, …) are thin
 * re-exports of feature screens; the real work lives in `src/features`.
 */
export default function RootLayout() {
  const [mocksReady, setMocksReady] = useState(false);

  useEffect(() => {
    void enableMocking().finally(() => setMocksReady(true));
  }, []);

  if (!mocksReady) return null;

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
