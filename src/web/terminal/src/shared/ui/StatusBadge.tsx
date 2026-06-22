import { StyleSheet, Text, View } from 'react-native';

import { fs, radius, type StatusKey } from '@/shared/theme/tokens';
import { useTheme } from '@/shared/theme/theme';

/**
 * StatusBadge — a dot + label pill. Status is NEVER conveyed by colour alone
 * (gloves + glare): the dot gives shape, the label gives words.
 */
export function StatusBadge({ kind, label }: { kind: StatusKey; label: string }) {
  const tone = useTheme().status[kind];
  return (
    <View style={[styles.badge, { backgroundColor: tone.bg }]}>
      <View style={[styles.dot, { backgroundColor: tone.fg }]} />
      <Text style={[styles.label, { color: tone.fg }]}>{label}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingVertical: 4,
    paddingHorizontal: 10,
    borderRadius: radius.pill,
    alignSelf: 'flex-start',
  },
  dot: { width: 8, height: 8, borderRadius: 4 },
  label: { fontSize: fs.xs, fontWeight: '700' },
});
