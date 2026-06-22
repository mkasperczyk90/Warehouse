import { Pressable, StyleSheet, Text } from 'react-native';

import { fs, radius, TAP } from '@/shared/theme/tokens';
import { useTheme } from '@/shared/theme/theme';

type Kind = 'primary' | 'ghost' | 'danger';

/**
 * BigActionButton — three buttons max per screen (confirm / exception /
 * alternative). Never a form. Tap target is the 56px terminal floor.
 *
 * `accent` recolours the primary fill so each screen can carry its own hue
 * (green receive, indigo pick, violet move…).
 */
export function BigActionButton({
  label,
  kind = 'primary',
  accent,
  disabled = false,
  onPress,
}: {
  label: string;
  kind?: Kind;
  accent?: string;
  /** Gate the action — e.g. a pick can't be confirmed until both scans land. */
  disabled?: boolean;
  onPress?: () => void;
}) {
  const t = useTheme();
  const primaryFill = accent ?? t.status.available.fg;

  let containerStyle;
  let textColor;
  if (kind === 'primary') {
    containerStyle = { backgroundColor: primaryFill };
    textColor = '#fff';
  } else if (kind === 'danger') {
    // Reserved for true hard-stops (e.g. QC-blocked). Routine exceptions use `ghost`.
    containerStyle = { borderWidth: 1, borderColor: t.status.blocked.fg };
    textColor = t.status.blocked.fg;
  } else {
    containerStyle = { borderWidth: 1, borderColor: accent ?? t.color.line };
    textColor = accent ?? t.color.inkSoft;
  }

  return (
    <Pressable
      style={({ pressed }) => [
        styles.btn,
        containerStyle,
        pressed && styles.pressed,
        disabled && styles.disabled,
      ]}
      onPress={onPress}
      disabled={disabled}
      accessibilityRole="button"
      accessibilityState={{ disabled }}
    >
      <Text style={[styles.label, { color: textColor }]}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  btn: {
    minHeight: TAP,
    borderRadius: radius.md,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 16,
  },
  pressed: { opacity: 0.85 },
  disabled: { opacity: 0.45 },
  label: { fontSize: fs.md, fontWeight: '700', textAlign: 'center' },
});
