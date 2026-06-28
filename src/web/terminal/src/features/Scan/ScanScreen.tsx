import { router } from 'expo-router';
import { useState } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';

import { BigActionButton, Card, Icon, ResourceView, ScanField, StatusBadge, TabScaffold } from '@/shared/ui';
import { useResource } from '@/core/api/useResource';
import { fs, s } from '@/shared/theme/tokens';
import { useTheme, useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';
import { getRecentScans, recordScan, resolveScan, type ScanResult } from './scan.model';

/** Terminal — Scan: the universal scan dispatcher. Scan anything → it routes. */
export function ScanScreen() {
  const recents = useResource(getRecentScans);
  return <ResourceView resource={recents}>{(data) => <ScanView initial={data} />}</ResourceView>;
}

function ScanView({ initial }: { initial: ScanResult[] }) {
  const theme = useTheme();
  const t = useT();
  const styles = useThemedStyles(makeStyles);
  const [results, setResults] = useState<ScanResult[]>(initial);
  const latest = results[0];
  const history = results.slice(1);

  const onScan = (code: string) => {
    recordScan(code); // remember on-device so the history survives leaving the screen
    setResults((prev) => [resolveScan(code), ...prev]);
  };

  return (
    <TabScaffold title={t('scan.title')} subtitle={t('scan.subtitle')} active="scan">
      <View style={styles.scanWrap}>
        <ScanField placeholder={t('scan.placeholder')} onScan={onScan} />
      </View>

      <ScrollView contentContainerStyle={styles.scroll} keyboardShouldPersistTaps="handled">
        {latest && (
          <Card style={styles.resultCard}>
            <View style={styles.resultHead}>
              <View style={styles.resultIcon}>
                <Icon name={latest.icon} size={40} color={theme.color.brand} />
              </View>
              <View style={styles.resultText}>
                <Text style={styles.resultKind}>{t(latest.titleKey)}</Text>
                <Text style={styles.resultCode}>{latest.code}</Text>
              </View>
            </View>
            <Text style={styles.resultSub}>{latest.kind === 'unknown' ? t('scan.unknownSub') : latest.subtitle}</Text>
            {latest.status && (
              <View style={styles.badgeRow}>
                <StatusBadge kind={latest.status.kind} label={t(`status.${latest.status.kind}`)} />
              </View>
            )}
            {latest.action ? (
              <View style={styles.actionWrap}>
                <BigActionButton
                  label={t(latest.action.key)}
                  accent={theme.color.brand}
                  onPress={() => router.push(latest.action!.route)}
                />
              </View>
            ) : (
              <Text style={styles.noAction}>{t('scan.noAction')}</Text>
            )}
          </Card>
        )}

        {history.length > 0 && (
          <>
            <Text style={styles.h1}>{t('scan.recent')}</Text>
            <Card style={styles.list}>
              {history.map((r, i) => (
                <View key={`${r.code}-${i}`} style={[styles.row, i < history.length - 1 && styles.rowBorder]}>
                  <View style={styles.rowIcon}>
                    <Icon name={r.icon} size={26} color={theme.color.inkFaint} />
                  </View>
                  <View style={styles.rowText}>
                    <Text style={styles.rowCode}>{r.code}</Text>
                    <Text style={styles.rowKind}>{t(r.titleKey)}</Text>
                  </View>
                  {r.status && <StatusBadge kind={r.status.kind} label={t(`status.${r.status.kind}`)} />}
                </View>
              ))}
            </Card>
          </>
        )}
      </ScrollView>
    </TabScaffold>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    scanWrap: { margin: s[5] },
    scroll: { paddingBottom: s[5] },

    resultCard: { marginHorizontal: s[5], padding: s[5] },
    resultHead: { flexDirection: 'row', alignItems: 'center', gap: s[4] },
    resultIcon: { width: 48, alignItems: 'center' },
    resultText: { flex: 1 },
    resultKind: { fontSize: fs.xs, color: t.color.inkFaint, textTransform: 'uppercase', letterSpacing: 1 },
    resultCode: { fontSize: fs.xl, fontWeight: '800', color: t.color.ink, marginTop: 2 },
    resultSub: { fontSize: fs.sm, color: t.color.inkSoft, marginTop: s[3] },
    badgeRow: { marginTop: s[3] },
    actionWrap: { marginTop: s[4] },
    noAction: { fontSize: fs.sm, color: t.color.inkFaint, marginTop: s[4] },

    h1: {
      fontSize: fs.sm,
      color: t.color.inkFaint,
      textTransform: 'uppercase',
      letterSpacing: 1,
      marginHorizontal: s[5],
      marginTop: s[5],
      marginBottom: s[2],
    },
    list: { marginHorizontal: s[5] },
    row: { flexDirection: 'row', alignItems: 'center', gap: s[4], paddingVertical: s[4], paddingHorizontal: s[5] },
    rowBorder: { borderBottomWidth: 1, borderBottomColor: t.color.lineSoft },
    rowIcon: { width: 32, alignItems: 'center' },
    rowText: { flex: 1 },
    rowCode: { fontSize: fs.sm, fontWeight: '700', color: t.color.ink },
    rowKind: { fontSize: fs.xs, color: t.color.inkFaint, marginTop: 2 },
  });
