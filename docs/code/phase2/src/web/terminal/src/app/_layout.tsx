import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useEffect, useState } from 'react';
import { Platform } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { color } from '@/shared/theme/tokens';
import { ThemeProvider } from '@/shared/theme/theme';
import { I18nProvider } from '@/shared/i18n/i18n';

/**
 * Until the Gateway is wired up, the terminal runs against MSW fixtures
 * (ADR-0006) — a Service Worker on web, a request interceptor on a handheld.
 * Going live is making this a no-op (or gating it on an env flag); the `fetch`
 * calls and the screens stay exactly as they are. We hold the first render
 * until the mocks are armed so no screen fetches before MSW can intercept.
 */
async function enableMocking() {
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
          <StatusBar style="light" />
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
        </ThemeProvider>
      </I18nProvider>
    </SafeAreaProvider>
  );
}
