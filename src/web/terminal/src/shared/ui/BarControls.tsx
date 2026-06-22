import { StyleSheet, View } from 'react-native';

import { s } from '@/shared/theme/tokens';
import { LangToggle } from './LangToggle';
import { ThemeToggle } from './ThemeToggle';

/** The bar's right-hand controls: language (PL/EN) + high-contrast toggle. */
export function BarControls() {
  return (
    <View style={styles.row}>
      <LangToggle />
      <ThemeToggle />
    </View>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', alignItems: 'center', gap: s[3] },
});
