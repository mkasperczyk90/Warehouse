import { StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { type TabKey } from '@/navigation/routes';
import { fs, s } from '@/shared/theme/tokens';
import { useThemedStyles, type Theme } from '@/shared/theme/theme';
import { BarControls } from './BarControls';
import { BottomNav } from './BottomNav';

/**
 * TabScaffold — the shared shape of a top-level tab root (Tasks / Scan /
 * Look up): a brand header with no back chevron, a flexible body, and the
 * persistent BottomNav pinned to the bottom.
 */
export function TabScaffold({
  title,
  subtitle,
  active,
  children,
}: {
  title: string;
  subtitle?: string;
  active: TabKey;
  children: React.ReactNode;
}) {
  const insets = useSafeAreaInsets();
  const styles = useThemedStyles(makeStyles);

  return (
    <View style={styles.root}>
      <View style={[styles.bar, { paddingTop: insets.top + s[4] }]}>
        <View style={styles.barText}>
          <Text style={styles.title}>{title}</Text>
          {subtitle ? <Text style={styles.subtitle}>{subtitle}</Text> : null}
        </View>
        <BarControls />
      </View>

      <View style={styles.body}>{children}</View>

      <View style={{ paddingBottom: insets.bottom }}>
        <BottomNav active={active} />
      </View>
    </View>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    root: { flex: 1, backgroundColor: t.color.canvas },
    bar: {
      backgroundColor: t.color.brand,
      paddingHorizontal: s[5],
      paddingBottom: s[4],
      flexDirection: 'row',
      alignItems: 'flex-start',
      justifyContent: 'space-between',
      gap: s[4],
    },
    barText: { flex: 1 },
    title: { color: '#fff', fontSize: fs.lg, fontWeight: '700' },
    subtitle: { color: '#fff', opacity: 0.85, fontSize: fs.xs, marginTop: 2 },
    body: { flex: 1 },
  });
