import { useState } from 'react';
import { StyleSheet, TextInput, View } from 'react-native';

import { fs, radius, s, shadow } from '@/shared/theme/tokens';
import { useTheme, useThemedStyles, type Theme } from '@/shared/theme/theme';
import { Icon } from './Icon';

type Variant = 'brand' | 'available' | 'move';

/**
 * ScanField — the king of the terminal. Always focused, the primary input;
 * the keyboard is the exception, not the rule. `onScan` fires on submit
 * (a hardware scanner sends an Enter after the payload).
 */
export function ScanField({
  placeholder,
  variant = 'brand',
  dashed = false,
  onScan,
}: {
  placeholder: string;
  variant?: Variant;
  dashed?: boolean;
  onScan?: (code: string) => void;
}) {
  const [value, setValue] = useState('');
  const t = useTheme();
  const styles = useThemedStyles(makeStyles);
  const variantColor: Record<Variant, string> = {
    brand: t.color.brand,
    available: t.status.available.fg,
    move: t.color.move,
  };
  const accent = variantColor[variant];

  return (
    <View
      style={[
        styles.scan,
        { borderColor: accent, borderStyle: dashed ? 'dashed' : 'solid' },
        !dashed && shadow.e1,
      ]}
    >
      <Icon name="scan" size={28} color={accent} />
      <TextInput
        style={styles.input}
        value={value}
        onChangeText={setValue}
        placeholder={placeholder}
        placeholderTextColor={t.color.inkFaint}
        autoFocus
        autoCorrect={false}
        autoCapitalize="characters"
        returnKeyType="done"
        onSubmitEditing={(e) => {
          const code = e.nativeEvent.text.trim();
          if (code) onScan?.(code);
          setValue('');
        }}
      />
    </View>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    scan: {
      backgroundColor: t.color.surface,
      borderWidth: 2,
      borderRadius: radius.md,
      paddingVertical: s[4],
      paddingHorizontal: s[5],
      flexDirection: 'row',
      alignItems: 'center',
      gap: s[4],
    },
    input: {
      flex: 1,
      fontSize: fs.md,
      color: t.color.ink,
      padding: 0,
    },
  });
