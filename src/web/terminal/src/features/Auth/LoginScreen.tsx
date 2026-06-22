import { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { BarControls, Icon, ScanField } from '@/shared/ui';
import { fs, radius, s, shadow } from '@/shared/theme/tokens';
import { useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';
import { useAuth } from '@/shared/auth/AuthContext';

/**
 * Badge-scan sign-in (terminal-0-login). A hardware badge reader types the id
 * and emits Enter, so the always-focused `ScanField` handles both a scan and
 * manual entry — `onScan` fires on Enter with a non-empty code. An unknown
 * badge surfaces an inline error; the field clears itself for the next scan.
 */
export function LoginScreen() {
  const t = useT();
  const styles = useThemedStyles(makeStyles);
  const insets = useSafeAreaInsets();
  const { signIn } = useAuth();
  const [error, setError] = useState(false);
  const [busy, setBusy] = useState(false);

  const onScan = async (badge: string) => {
    if (busy) return;
    setBusy(true);
    setError(false);
    try {
      await signIn(badge);
    } catch {
      setError(true);
    } finally {
      setBusy(false);
    }
  };

  return (
    <View style={styles.root}>
      <View style={[styles.bar, { paddingTop: insets.top + s[4] }]}>
        <Text style={styles.brand}>{t('app.name')}</Text>
        <BarControls />
      </View>

      <View style={styles.body}>
        <View style={styles.badge}>
          <Icon name="scan" size={56} color="#fff" />
        </View>

        <Text style={styles.title}>{t('login.title')}</Text>
        <Text style={styles.subtitle}>{t('login.subtitle')}</Text>

        <View style={styles.scanWrap}>
          <ScanField placeholder={t('login.badgePlaceholder')} onScan={onScan} />
        </View>

        {error ? (
          <Text style={styles.error} accessibilityRole="alert">
            {t('login.error')}
          </Text>
        ) : null}

        <Text style={styles.hint}>{t('login.hint')}</Text>
      </View>
    </View>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    root: { flex: 1, backgroundColor: t.color.canvas },
    bar: {
      backgroundColor: t.color.brand,
      paddingHorizontal: s[5],
      paddingBottom: s[4],
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      gap: s[4],
    },
    brand: { color: '#fff', fontSize: fs.lg, fontWeight: '800' },

    body: { flex: 1, justifyContent: 'center', paddingHorizontal: s[5], gap: s[3] },
    badge: {
      alignSelf: 'center',
      width: 96,
      height: 96,
      borderRadius: radius.lg,
      backgroundColor: t.color.brand,
      alignItems: 'center',
      justifyContent: 'center',
      marginBottom: s[4],
      ...shadow.e2,
    },
    title: { fontSize: fs.xl, fontWeight: '700', color: t.color.ink, textAlign: 'center' },
    subtitle: {
      fontSize: fs.md,
      color: t.color.inkSoft,
      textAlign: 'center',
      marginBottom: s[3],
    },
    scanWrap: { marginTop: s[2] },
    error: {
      fontSize: fs.sm,
      color: t.status.blocked.fg,
      fontWeight: '700',
      textAlign: 'center',
    },
    hint: { fontSize: fs.xs, color: t.color.inkFaint, textAlign: 'center', marginTop: s[2] },
  });
