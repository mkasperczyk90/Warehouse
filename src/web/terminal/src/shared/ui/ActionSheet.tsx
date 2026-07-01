import { Modal, Pressable, StyleSheet, Text, View } from 'react-native';

import { fs, radius, s, shadow, TAP } from '@/shared/theme/tokens';
import { useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';

export interface SheetOption {
  key: string;
  label: string;
  /** Optional second line — e.g. "routes to QC quarantine". */
  hint?: string;
  /** Loudest treatment for a hard / QC-bound choice. */
  danger?: boolean;
}

/**
 * ActionSheet — a bottom-sheet reason picker for the unhappy paths (report a
 * discrepancy, short pick…). The terminal's answer to the admin's reason modal:
 * a required choice before the write posts, but as big one-tap rows for gloves
 * rather than a form. Cancel leaves the task untouched.
 */
export function ActionSheet({
  visible,
  title,
  options,
  onSelect,
  onClose,
}: {
  visible: boolean;
  title: string;
  options: SheetOption[];
  onSelect: (key: string) => void;
  onClose: () => void;
}) {
  const styles = useThemedStyles(makeStyles);
  const t = useT();

  return (
    <Modal visible={visible} transparent animationType="slide" onRequestClose={onClose}>
      <Pressable style={styles.backdrop} onPress={onClose}>
        <Pressable style={styles.sheet} onPress={() => {}}>
          <Text style={styles.title}>{title}</Text>

          <View style={styles.options}>
            {options.map((o) => (
              <Pressable
                key={o.key}
                style={({ pressed }) => [
                  styles.option,
                  o.danger && styles.optionDanger,
                  pressed && styles.pressed,
                ]}
                onPress={() => onSelect(o.key)}
                accessibilityRole="button"
              >
                <Text style={[styles.optionLabel, o.danger && styles.optionLabelDanger]}>
                  {o.label}
                </Text>
                {o.hint ? <Text style={styles.optionHint}>{o.hint}</Text> : null}
              </Pressable>
            ))}
          </View>

          <Pressable
            style={({ pressed }) => [styles.cancel, pressed && styles.pressed]}
            onPress={onClose}
            accessibilityRole="button"
          >
            <Text style={styles.cancelText}>{t('common.cancel')}</Text>
          </Pressable>
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
    title: { fontSize: fs.md, fontWeight: '700', color: t.color.ink, marginBottom: s[4] },
    options: { gap: s[3] },
    option: {
      minHeight: TAP,
      borderWidth: 1,
      borderColor: t.color.line,
      backgroundColor: t.color.canvas,
      borderRadius: radius.md,
      paddingVertical: s[3],
      paddingHorizontal: s[4],
      justifyContent: 'center',
    },
    optionDanger: { borderColor: t.status.blocked.fg, backgroundColor: t.status.blocked.bg },
    optionLabel: { fontSize: fs.md, fontWeight: '700', color: t.color.ink },
    optionLabelDanger: { color: t.status.blocked.fg },
    optionHint: { fontSize: fs.xs, color: t.color.inkFaint, marginTop: 2 },
    cancel: { minHeight: TAP, alignItems: 'center', justifyContent: 'center', marginTop: s[4] },
    cancelText: { fontSize: fs.md, fontWeight: '700', color: t.color.inkSoft },
    pressed: { opacity: 0.7 },
  });
