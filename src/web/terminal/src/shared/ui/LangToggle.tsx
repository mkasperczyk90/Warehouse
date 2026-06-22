import { Pressable, StyleSheet, Text } from 'react-native';

import { fs, radius } from '@/shared/theme/tokens';
import { useI18n } from '@/shared/i18n/i18n';

/**
 * LangToggle — the PL/EN control in the bar. The terminal defaults to Polish
 * (floor staff); this flips to English and is remembered per device.
 */
export function LangToggle() {
  const { locale, toggle, t } = useI18n();
  return (
    <Pressable
      onPress={toggle}
      hitSlop={12}
      style={styles.btn}
      accessibilityRole="button"
      accessibilityLabel={t('a11y.toggleLanguage')}
    >
      <Text style={styles.text}>{locale.toUpperCase()}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  btn: {
    paddingVertical: 4,
    paddingHorizontal: 8,
    borderRadius: radius.pill,
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.6)',
  },
  text: { color: '#fff', fontSize: fs.xs, fontWeight: '800' },
});
