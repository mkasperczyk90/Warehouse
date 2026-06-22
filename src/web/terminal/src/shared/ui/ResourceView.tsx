import type { ReactNode } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';

import type { Resource } from '@/core/api/useResource';
import { fs, radius, s } from '@/shared/theme/tokens';
import { useTheme, useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';

/**
 * Renders the three states of one async read: a centered spinner while loading,
 * a retry prompt on error, and the data via a render-prop once it arrives. This
 * keeps the data-dependent hooks (useState seeded from a fixture) inside the
 * child component, so they are only ever called when the data is present.
 */
export function ResourceView<T>({
  resource,
  children,
}: {
  resource: Resource<T>;
  children: (data: T) => ReactNode;
}) {
  const theme = useTheme();
  const t = useT();
  const styles = useThemedStyles(makeStyles);

  if (resource.data !== undefined) return <>{children(resource.data)}</>;

  if (resource.error) {
    return (
      <View style={styles.center}>
        <Text style={styles.errText}>{t('common.loadError')}</Text>
        <Pressable style={styles.retry} onPress={resource.reload} accessibilityRole="button">
          <Text style={styles.retryText}>{t('common.retry')}</Text>
        </Pressable>
      </View>
    );
  }

  return (
    <View style={styles.center}>
      <ActivityIndicator size="large" color={theme.color.brand} />
      <Text style={styles.loadText}>{t('common.loading')}</Text>
    </View>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    center: { flex: 1, alignItems: 'center', justifyContent: 'center', gap: s[4], padding: s[5], backgroundColor: t.color.canvas },
    loadText: { fontSize: fs.sm, color: t.color.inkFaint },
    errText: { fontSize: fs.md, color: t.color.ink, textAlign: 'center' },
    retry: {
      minHeight: 48,
      justifyContent: 'center',
      paddingHorizontal: s[6],
      borderRadius: radius.md,
      backgroundColor: t.color.brand,
    },
    retryText: { color: '#fff', fontSize: fs.md, fontWeight: '700' },
  });
