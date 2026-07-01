import { StyleSheet, Text, View } from 'react-native';

import { fs, s } from '@/shared/theme/tokens';
import { useThemedStyles, type Theme } from '@/shared/theme/theme';

/**
 * CheckRow — a green-tick assertion that the system has validated a hard
 * invariant (temperature compatible, capacity OK). On put-away and move these
 * make the environment-compatibility invariant visible rather than hidden.
 */
export function CheckRow({ label }: { label: string }) {
  const styles = useThemedStyles(makeStyles);
  return (
    <View style={styles.row}>
      <Text style={styles.ok}>✓</Text>
      <Text style={styles.label}>{label}</Text>
    </View>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    row: { flexDirection: 'row', alignItems: 'center', gap: s[3] },
    ok: { color: t.status.available.fg, fontWeight: '800', fontSize: fs.md },
    label: { fontSize: fs.sm, color: t.color.inkSoft, flex: 1 },
  });
