import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';

import { fs, radius, s, shadow } from '@/shared/theme/tokens';
import { useTheme, useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';
import { Icon } from './Icon';

/**
 * SearchField — the keyboard-driven counterpart to ScanField. Used in Look up,
 * where the operator queries without a physical barcode. Read-only intent: it
 * filters, it never writes. A clear (×) button resets the query.
 */
export function SearchField({
  value,
  onChangeText,
  placeholder,
}: {
  value: string;
  onChangeText: (next: string) => void;
  placeholder: string;
}) {
  const theme = useTheme();
  const t = useT();
  const styles = useThemedStyles(makeStyles);
  return (
    <View style={[styles.field, shadow.e1]}>
      <Icon name="search" size={26} color={theme.color.inkFaint} />
      <TextInput
        style={styles.input}
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder}
        placeholderTextColor={theme.color.inkFaint}
        autoCorrect={false}
        returnKeyType="search"
        clearButtonMode="while-editing"
      />
      {value.length > 0 && (
        <Pressable
          onPress={() => onChangeText('')}
          hitSlop={12}
          accessibilityLabel={t('a11y.clearSearch')}
        >
          <Text style={styles.clear}>×</Text>
        </Pressable>
      )}
    </View>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    field: {
      backgroundColor: t.color.surface,
      borderWidth: 2,
      borderColor: t.color.line,
      borderRadius: radius.md,
      paddingVertical: s[3],
      paddingHorizontal: s[5],
      flexDirection: 'row',
      alignItems: 'center',
      gap: s[4],
    },
    input: { flex: 1, fontSize: fs.md, color: t.color.ink, padding: 0 },
    clear: { fontSize: fs.xl, color: t.color.inkFaint, lineHeight: fs.xl },
  });
