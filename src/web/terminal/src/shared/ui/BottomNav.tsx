import { router } from 'expo-router';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { ROUTES, type TabKey } from '@/navigation/routes';
import { fs, s } from '@/shared/theme/tokens';
import { useTheme, useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';
import { Icon, type IconName } from './Icon';

interface NavItem {
  key: TabKey | 'more';
  icon: IconName;
  label: string;
  route?: (typeof ROUTES)[keyof typeof ROUTES];
}

const ITEMS: NavItem[] = [
  { key: 'tasks', icon: 'tasks', label: 'Tasks', route: ROUTES.hub },
  { key: 'scan', icon: 'scan', label: 'Scan', route: ROUTES.scan },
  { key: 'lookup', icon: 'search', label: 'Look up', route: ROUTES.lookup },
  { key: 'more', icon: 'more', label: 'More' }, // no screen yet — dimmed
];

/**
 * BottomNav — the operator's persistent tab bar across the three top-level
 * roots (Tasks / Scan / Look up). `router.navigate` gives tab-like behaviour:
 * switching to a root already in history returns to it instead of stacking a
 * duplicate. "More" has no screen yet, so it stays dimmed and inert.
 */
export function BottomNav({ active }: { active: TabKey }) {
  const theme = useTheme();
  const t = useT();
  const styles = useThemedStyles(makeStyles);
  return (
    <View style={styles.nav}>
      {ITEMS.map((it) => {
        const isActive = it.key === active;
        const disabled = !it.route;
        return (
          <Pressable
            key={it.key}
            style={[styles.item, disabled && styles.dim]}
            disabled={disabled || isActive}
            onPress={() => it.route && router.navigate(it.route)}
            accessibilityRole="tab"
            accessibilityState={{ selected: isActive, disabled }}
          >
            <Icon name={it.icon} size={24} color={isActive ? theme.color.brand : theme.color.inkFaint} />
            <Text style={[styles.label, isActive && styles.onText]}>{t(`nav.${it.key}`)}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    nav: {
      flexDirection: 'row',
      borderTopWidth: 1,
      borderTopColor: t.color.line,
      backgroundColor: t.color.surface,
    },
    item: { flex: 1, alignItems: 'center', paddingVertical: s[4], gap: 2 },
    label: { fontSize: fs.xs, color: t.color.inkFaint },
    onText: { color: t.color.brand, fontWeight: '700' },
    dim: { opacity: 0.4 },
  });
