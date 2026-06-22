import { useState } from 'react';
import { Modal, Pressable, StyleSheet, Text, View } from 'react-native';

import { fs, radius, s, shadow, TAP } from '@/shared/theme/tokens';
import { useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';

const KEYS = ['1', '2', '3', '4', '5', '6', '7', '8', '9', 'del', '0', 'ok'] as const;

/**
 * Keypad — a bottom-sheet numeric pad for real counts.
 *
 * The stepper is fine for ±1, but a true discrepancy (e.g. 240 → 218) is a
 * deliberate keypad entry, not twenty taps. A one-tap "= expected" default
 * keeps the common case (count matches the ASN) instant. Large keys for gloves.
 */
export function Keypad({
  visible,
  value,
  expected,
  unit,
  label,
  onConfirm,
  onClose,
}: {
  visible: boolean;
  value: number;
  expected: number;
  unit: string;
  label?: string;
  onConfirm: (next: number) => void;
  onClose: () => void;
}) {
  const styles = useThemedStyles(makeStyles);
  const t = useT();
  // null until the operator types a fresh value; otherwise the digits so far.
  const [entry, setEntry] = useState<string | null>(null);
  const shown = entry ?? String(value);

  function reset() {
    setEntry(null);
  }
  function close() {
    reset();
    onClose();
  }
  function press(k: (typeof KEYS)[number]) {
    if (k === 'ok') {
      onConfirm(entry !== null && entry !== '' ? Number(entry) : value);
      reset();
      return;
    }
    if (k === 'del') {
      setEntry((entry ?? String(value)).slice(0, -1));
      return;
    }
    setEntry(((entry ?? '') + k).replace(/^0+(?=\d)/, ''));
  }

  return (
    <Modal visible={visible} transparent animationType="slide" onRequestClose={close}>
      <Pressable style={styles.backdrop} onPress={close}>
        {/* swallow taps inside the sheet so they don't dismiss it */}
        <Pressable style={styles.sheet} onPress={() => {}}>
          {label ? <Text style={styles.ctx}>{label}</Text> : null}
          <Text style={styles.val}>{shown || '0'}</Text>

          <Pressable
            style={({ pressed }) => [styles.exp, pressed && styles.pressed]}
            onPress={() => setEntry(String(expected))}
            accessibilityRole="button"
          >
            <Text style={styles.expText}>{t('keypad.expected', { n: expected, unit })}</Text>
          </Pressable>

          <View style={styles.grid}>
            {KEYS.map((k) => (
              <Pressable
                key={k}
                style={({ pressed }) => [styles.key, k === 'ok' && styles.keyOk, pressed && styles.pressed]}
                onPress={() => press(k)}
                accessibilityRole="button"
                accessibilityLabel={k === 'del' ? t('a11y.backspace') : k === 'ok' ? t('a11y.confirmQty') : k}
              >
                <Text style={[styles.keyText, k === 'ok' && styles.keyOkText, k === 'del' && styles.keyDelText]}>
                  {k === 'del' ? '⌫' : k === 'ok' ? '✓' : k}
                </Text>
              </Pressable>
            ))}
          </View>
        </Pressable>
      </Pressable>
    </Modal>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    backdrop: { flex: 1, backgroundColor: 'rgba(0,0,0,0.45)', justifyContent: 'flex-end' },
    sheet: {
      backgroundColor: t.color.surface,
      borderTopLeftRadius: radius.lg,
      borderTopRightRadius: radius.lg,
      padding: s[5],
      ...shadow.e2,
    },
    ctx: { fontSize: fs.xs, color: t.color.inkFaint },
    val: {
      fontSize: fs['2xl'],
      fontWeight: '800',
      textAlign: 'center',
      fontVariant: ['tabular-nums'],
      color: t.color.ink,
      paddingVertical: s[3],
    },
    exp: {
      minHeight: TAP,
      borderWidth: 1,
      borderColor: t.status.transit.fg,
      backgroundColor: t.status.transit.bg,
      borderRadius: radius.md,
      alignItems: 'center',
      justifyContent: 'center',
      marginBottom: s[4],
    },
    expText: { color: t.status.transit.fg, fontSize: fs.sm, fontWeight: '700' },
    grid: { flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'space-between', rowGap: s[3] },
    key: {
      width: '31%',
      minHeight: TAP,
      borderRadius: radius.md,
      borderWidth: 1,
      borderColor: t.color.line,
      backgroundColor: t.color.canvas,
      alignItems: 'center',
      justifyContent: 'center',
    },
    keyOk: { backgroundColor: t.status.available.fg, borderColor: t.status.available.fg },
    keyText: { fontSize: fs.xl, fontWeight: '700', color: t.color.ink },
    keyOkText: { color: '#fff' },
    keyDelText: { color: t.status.blocked.fg },
    pressed: { opacity: 0.7 },
  });
