import { router } from 'expo-router';
import { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';

import { BigActionButton, Card, ResourceView, ScanField, ScreenScaffold } from '@/shared/ui';
import { useResource } from '@/core/api/useResource';
import { fs, radius, s } from '@/shared/theme/tokens';
import { useTheme, useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';
import { closePackage, getPackJob, type PackJob } from './packing.model';

/** Terminal — Packing (terminal-6-pack · UC-11). */
export function PackingScreen() {
  const pack = useResource(getPackJob);
  return <ResourceView resource={pack}>{(data) => <PackingView pack={data} />}</ResourceView>;
}

function PackingView({ pack }: { pack: PackJob }) {
  const theme = useTheme();
  const t = useT();
  const styles = useThemedStyles(makeStyles);
  const [pending, setPending] = useState(false);
  // Splitting an order across packages is a terminal-side affordance (the backend ships one shipment per
  // order for now), so the package number is local UI state.
  const [pkgNo, setPkgNo] = useState(1);

  async function close() {
    setPending(true);
    try {
      await closePackage();
      router.dismissAll();
    } catch {
      setPending(false);
    }
  }

  function addAnother() {
    setPkgNo((n) => n + 1);
  }

  return (
    <ScreenScaffold
      title={t('pack.title', { order: pack.order })}
      subtitle={t('pack.customer', { customer: pack.customer })}
      actions={
        <>
          <BigActionButton
            label={t('pack.close')}
            accent={theme.status.available.fg}
            disabled={pending}
            onPress={close}
          />
          <BigActionButton
            label={t('pack.add')}
            kind="ghost"
            disabled={pending}
            onPress={addAnother}
          />
        </>
      }
    >
      {/* Active package */}
      <View style={styles.pkg}>
        <Text style={styles.pkgCap}>{t('pack.active')}</Text>
        <Text style={styles.pkgId}>PKG {pkgNo}</Text>
      </View>

      <Text style={styles.h1}>{t('pack.heading')}</Text>
      <Card style={styles.items}>
        {pack.lines.map((li, i) => (
          <View
            key={`${li.name}-${li.lot}`}
            style={[styles.li, i < pack.lines.length - 1 && styles.liBorder]}
          >
            <View style={[styles.tick, li.done ? styles.tickDone : styles.tickTodo]}>
              <Text
                style={[
                  styles.tickText,
                  { color: li.done ? theme.status.available.fg : theme.color.inkFaint },
                ]}
              >
                {li.done ? '✓' : String(li.remaining ?? '')}
              </Text>
            </View>
            <View style={styles.nm}>
              <Text style={styles.nmName}>{li.name}</Text>
              <Text style={styles.nmLot}>{li.lot}</Text>
            </View>
            <Text style={styles.q}>{li.qty}</Text>
          </View>
        ))}
      </Card>

      <View style={styles.scanWrap}>
        <ScanField placeholder={t('pack.scan')} />
      </View>
    </ScreenScaffold>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    pkg: {
      margin: s[5],
      backgroundColor: t.status.reserved.bg,
      borderWidth: 2,
      borderColor: t.status.reserved.fg,
      borderRadius: radius.lg,
      padding: s[5],
      alignItems: 'center',
    },
    pkgCap: {
      fontSize: fs.xs,
      color: t.status.reserved.fg,
      textTransform: 'uppercase',
      letterSpacing: 1,
    },
    pkgId: { fontSize: fs.xl, fontWeight: '800', color: t.status.reserved.fg, marginTop: 2 },

    h1: {
      fontSize: fs.sm,
      color: t.color.inkFaint,
      textTransform: 'uppercase',
      letterSpacing: 1,
      marginHorizontal: s[5],
      marginBottom: s[2],
    },
    items: { marginHorizontal: s[5], marginBottom: s[4] },
    li: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: s[3],
      paddingVertical: s[4],
      paddingHorizontal: s[5],
    },
    liBorder: { borderBottomWidth: 1, borderBottomColor: t.color.lineSoft },
    tick: {
      width: 28,
      height: 28,
      borderRadius: 14,
      alignItems: 'center',
      justifyContent: 'center',
    },
    tickDone: { backgroundColor: t.status.available.bg },
    tickTodo: {
      backgroundColor: t.color.canvas,
      borderWidth: 1,
      borderColor: t.color.line,
      borderStyle: 'dashed',
    },
    tickText: { fontWeight: '800', fontSize: fs.sm },
    nm: { flex: 1 },
    nmName: { fontSize: fs.sm, color: t.color.ink },
    nmLot: { fontSize: fs.xs, color: t.color.inkFaint, marginTop: 2 },
    q: { fontWeight: '800', fontVariant: ['tabular-nums'], color: t.color.ink },

    scanWrap: { margin: s[5] },
  });
