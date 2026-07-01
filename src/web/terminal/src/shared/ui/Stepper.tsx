import { Pressable, StyleSheet, Text, View } from 'react-native';

import { fs, radius, s, TAP } from '@/shared/theme/tokens';
import { useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';

/**
 * Quantity stepper — large +/- targets either side of a tabular number.
 * Used for the counted/move quantity on receipt and move screens. When
 * `onPressValue` is set the number itself is tappable, opening the keypad.
 */
export function Stepper({
  value,
  onChange,
  min = 0,
  step = 1,
  onPressValue,
}: {
  value: number;
  onChange: (next: number) => void;
  min?: number;
  step?: number;
  /** When set, the number itself is tappable — opens the numeric keypad for real counts. */
  onPressValue?: () => void;
}) {
  const styles = useThemedStyles(makeStyles);
  const t = useT();
  return (
    <View style={styles.stepper}>
      <Pressable
        style={({ pressed }) => [styles.btn, pressed && styles.pressed]}
        onPress={() => onChange(Math.max(min, value - step))}
        accessibilityRole="button"
        accessibilityLabel={t('a11y.decrease')}
      >
        <Text style={styles.sign}>−</Text>
      </Pressable>
      {onPressValue ? (
        <Pressable
          onPress={onPressValue}
          accessibilityRole="button"
          accessibilityLabel={t('a11y.enterQty')}
        >
          <Text style={[styles.num, styles.numTappable]}>{value}</Text>
        </Pressable>
      ) : (
        <Text style={styles.num}>{value}</Text>
      )}
      <Pressable
        style={({ pressed }) => [styles.btn, pressed && styles.pressed]}
        onPress={() => onChange(value + step)}
        accessibilityRole="button"
        accessibilityLabel={t('a11y.increase')}
      >
        <Text style={styles.sign}>+</Text>
      </Pressable>
    </View>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    stepper: { flexDirection: 'row', alignItems: 'center', gap: s[3] },
    btn: {
      width: TAP,
      height: TAP,
      borderRadius: radius.md,
      borderWidth: 1,
      borderColor: t.color.line,
      backgroundColor: t.color.canvas,
      alignItems: 'center',
      justifyContent: 'center',
    },
    pressed: { opacity: 0.7 },
    sign: { fontSize: fs.lg, fontWeight: '700', color: t.color.ink },
    num: {
      fontSize: fs.xl,
      fontWeight: '800',
      minWidth: 64,
      textAlign: 'center',
      fontVariant: ['tabular-nums'],
      color: t.color.ink,
    },
    numTappable: {
      borderBottomWidth: 2,
      borderBottomColor: t.color.line,
      borderStyle: 'dashed',
      paddingHorizontal: s[2],
    },
  });
