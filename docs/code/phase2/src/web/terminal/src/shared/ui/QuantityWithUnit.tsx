import { StyleSheet, Text, type TextStyle } from 'react-native';

import { fs } from '@/shared/theme/tokens';
import { useTheme, useThemedStyles, type Theme } from '@/shared/theme/theme';

/**
 * QuantityWithUnit — never a bare number. Always a unit, echoing the domain's
 * unit-safe `Quantity`. Tabular numerals so digits don't jump.
 */
export function QuantityWithUnit({
  value,
  unit,
  size = fs.md,
  tone,
}: {
  value: number | string;
  unit: string;
  size?: number;
  tone?: string;
}) {
  const t = useTheme();
  const styles = useThemedStyles(makeStyles);
  const numStyle: TextStyle = { fontSize: size, color: tone ?? t.color.ink };
  return (
    <Text style={[styles.qty, numStyle]}>
      {value}
      <Text style={[styles.unit, { fontSize: Math.round(size * 0.55) }]}> {unit}</Text>
    </Text>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    qty: {
      fontWeight: '800',
      fontVariant: ['tabular-nums'],
    },
    unit: {
      fontWeight: '500',
      color: t.color.inkFaint,
    },
  });
