import { router } from 'expo-router';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { fs, s } from '@/shared/theme/tokens';
import { useTheme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';
import { BarControls } from './BarControls';
import { Icon } from './Icon';

/**
 * TopBar — the screen header. On task screens it carries a back chevron, the
 * screen title and a step subtitle (e.g. "Line 3 of 8"). `accent` lets the
 * Move-stock flow paint the bar violet. The right slot defaults to the
 * high-contrast (glare) toggle; pass `right` to override.
 */
export function TopBar({
  title,
  subtitle,
  back = true,
  accent,
  right,
}: {
  title: string;
  subtitle?: string;
  back?: boolean;
  accent?: string;
  right?: React.ReactNode;
}) {
  const theme = useTheme();
  const t = useT();
  return (
    <View style={[styles.bar, { backgroundColor: accent ?? theme.color.brand }]}>
      {back && (
        <Pressable
          onPress={() => router.back()}
          hitSlop={12}
          accessibilityRole="button"
          accessibilityLabel={t('a11y.back')}
        >
          <Icon name="back" size={fs.xl} color="#fff" />
        </Pressable>
      )}
      <View style={styles.ttl}>
        <Text style={styles.title}>{title}</Text>
        {subtitle ? <Text style={styles.subtitle}>{subtitle}</Text> : null}
      </View>
      {right ?? <BarControls />}
    </View>
  );
}

const styles = StyleSheet.create({
  bar: {
    paddingVertical: s[4],
    paddingHorizontal: s[5],
    flexDirection: 'row',
    alignItems: 'center',
    gap: s[4],
  },
  ttl: { flex: 1 },
  title: { fontSize: fs.md, color: '#fff', fontWeight: '700' },
  subtitle: { fontSize: fs.xs, color: '#fff', opacity: 0.85, marginTop: 2 },
});
