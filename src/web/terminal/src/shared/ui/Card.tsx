import { StyleSheet, View, type ViewProps } from 'react-native';

import { radius, shadow } from '@/shared/theme/tokens';
import { useThemedStyles, type Theme } from '@/shared/theme/theme';

/** Card — the white, rounded, lightly-elevated surface most screens build on. */
export function Card({ style, children, ...rest }: ViewProps) {
  const styles = useThemedStyles(makeStyles);
  return (
    <View style={[styles.card, style]} {...rest}>
      {children}
    </View>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    card: {
      backgroundColor: t.color.surface,
      borderRadius: radius.lg,
      overflow: 'hidden',
      ...shadow.e1,
    },
  });
