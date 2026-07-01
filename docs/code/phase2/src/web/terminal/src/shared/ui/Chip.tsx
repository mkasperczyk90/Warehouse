import { StyleSheet, Text, View } from 'react-native';

import { fs, radius } from '@/shared/theme/tokens';
import { useThemedStyles, type Theme } from '@/shared/theme/theme';

/**
 * Chip — a small pill for batch / BBE / quantity context. The `cold` variant
 * makes the cold-chain requirement (environment-compatibility invariant)
 * visible, not hidden.
 */
export function Chip({ label, cold = false }: { label: string; cold?: boolean }) {
  const styles = useThemedStyles(makeStyles);
  return (
    <View style={[styles.chip, cold && styles.cold]}>
      <Text style={[styles.label, cold && styles.coldLabel]}>{label}</Text>
    </View>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    chip: {
      backgroundColor: t.color.canvas,
      borderWidth: 1,
      borderColor: t.color.line,
      borderRadius: radius.pill,
      paddingVertical: 3,
      paddingHorizontal: 10,
      alignSelf: 'flex-start',
    },
    cold: { backgroundColor: t.status.reserved.bg, borderColor: 'transparent' },
    label: { fontSize: fs.xs, color: t.color.ink },
    coldLabel: { color: t.status.reserved.fg, fontWeight: '600' },
  });
