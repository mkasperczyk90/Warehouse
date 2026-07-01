import { ScrollView, StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { s } from '@/shared/theme/tokens';
import { useTheme, useThemedStyles, type Theme } from '@/shared/theme/theme';
import { TopBar } from './TopBar';

/**
 * ScreenScaffold — the shared shape of every task screen: a coloured TopBar,
 * a scrollable body, and a sticky actions footer (the 2–3 big buttons that
 * never scroll out of thumb reach).
 */
export function ScreenScaffold({
  title,
  subtitle,
  accent,
  children,
  actions,
}: {
  title: string;
  subtitle?: string;
  accent?: string;
  children: React.ReactNode;
  actions: React.ReactNode;
}) {
  const insets = useSafeAreaInsets();
  const t = useTheme();
  const styles = useThemedStyles(makeStyles);
  const barColor = accent ?? t.color.brand;

  return (
    <View style={styles.root}>
      <View style={{ paddingTop: insets.top, backgroundColor: barColor }}>
        <TopBar title={title} subtitle={subtitle} accent={barColor} />
      </View>

      <ScrollView contentContainerStyle={styles.body} keyboardShouldPersistTaps="handled">
        {children}
      </ScrollView>

      <View style={[styles.actions, { paddingBottom: insets.bottom + s[5] }]}>{actions}</View>
    </View>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    root: { flex: 1, backgroundColor: t.color.canvas },
    body: { paddingBottom: s[5] },
    actions: {
      paddingHorizontal: s[5],
      paddingTop: s[5],
      gap: s[3],
      backgroundColor: t.color.surface,
      borderTopWidth: 1,
      borderTopColor: t.color.line,
    },
  });
