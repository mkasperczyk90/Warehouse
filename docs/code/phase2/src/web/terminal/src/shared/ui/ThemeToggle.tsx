import { Pressable, StyleSheet } from 'react-native';

import { fs, radius } from '@/shared/theme/tokens';
import { useThemeToggle } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';
import { Icon } from './Icon';

/**
 * ThemeToggle — the ◐ control in the bar that flips the terminal to the
 * high-contrast (glare) theme. Glare on a bright cold-store floor is a primary
 * operator driver, so the toggle lives in every bar; the choice is persisted.
 */
export function ThemeToggle() {
  const { hc, toggle } = useThemeToggle();
  const t = useT();
  return (
    <Pressable
      onPress={toggle}
      hitSlop={12}
      style={[styles.btn, hc && styles.on]}
      accessibilityRole="button"
      accessibilityState={{ selected: hc }}
      accessibilityLabel={t('a11y.toggleContrast')}
    >
      <Icon name="contrast" size={fs.xl} color="#fff" />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  btn: { padding: 4, borderRadius: radius.pill },
  on: { backgroundColor: 'rgba(255,255,255,0.28)' },
});
