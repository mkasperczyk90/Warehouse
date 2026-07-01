import { router } from 'expo-router';
import { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';

import { ROUTES } from '@/navigation/routes';
import {
  ActionSheet,
  BigActionButton,
  Card,
  QuantityWithUnit,
  ResourceView,
  ScanField,
  ScreenScaffold,
  StatusBadge,
} from '@/shared/ui';
import { useResource } from '@/core/api/useResource';
import { fs, radius, s } from '@/shared/theme/tokens';
import { useTheme, useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';
import {
  confirmPick,
  getPickStep,
  shortPick,
  type PickStep,
  type ShortReason,
} from './picking.model';

/** Terminal — Picking (terminal-4-pick · UC-10). */
export function PickingScreen() {
  const pick = useResource(getPickStep);
  return (
    <ResourceView resource={pick}>
      {(data) => <PickingView pick={data} reload={pick.reload} />}
    </ResourceView>
  );
}

function PickingView({ pick, reload }: { pick: PickStep; reload: () => void }) {
  const theme = useTheme();
  const t = useT();
  const styles = useThemedStyles(makeStyles);
  const ratio = pick.picked / pick.total;
  const left = pick.total - pick.picked;

  // The scan IS the commit: location then product must be scanned before we can confirm —
  // never confirm a pick from memory ("believe scans, not memory").
  const steps = [
    { label: t('pick.scanLocation'), detail: pick.location },
    { label: t('pick.scanProduct'), detail: `SKU ${pick.sku}` },
  ];
  const prompts = [t('pick.promptLocation'), t('pick.promptProduct')];
  const [scanned, setScanned] = useState(0);
  const [sheetOpen, setSheetOpen] = useState(false);
  const [pending, setPending] = useState(false);
  const allScanned = scanned >= steps.length;

  async function confirm() {
    setPending(true);
    try {
      await confirmPick();
      router.push(ROUTES.pack);
    } catch {
      setPending(false);
    }
  }

  // Short pick → replan onto the next FEFO batch/location; rescan from the top.
  async function short(reason: ShortReason) {
    setSheetOpen(false);
    setPending(true);
    try {
      await shortPick(reason);
      setScanned(0);
      reload();
    } finally {
      setPending(false);
    }
  }

  return (
    <ScreenScaffold
      title={t('pick.title', { wave: pick.wave })}
      subtitle={t('pick.order', { order: pick.order })}
      actions={
        <>
          <BigActionButton
            label={allScanned ? t('pick.confirm') : t('pick.scanToConfirm')}
            accent={theme.color.brand}
            disabled={!allScanned || pending}
            onPress={confirm}
          />
          {/* routine exception — neutral, not a red alarm */}
          <BigActionButton
            label={t('pick.short')}
            kind="ghost"
            disabled={pending}
            onPress={() => setSheetOpen(true)}
          />
        </>
      }
    >
      {/* Progress */}
      <View style={styles.prog}>
        <Text style={styles.progLbl}>{`${pick.picked}/${pick.total}`}</Text>
        <View style={styles.track}>
          <View style={[styles.fill, { width: `${ratio * 100}%` }]} />
        </View>
        <Text style={styles.progLbl}>{t('pick.left', { n: left })}</Text>
      </View>

      {/* Go-to location */}
      <View style={styles.goloc}>
        <Text style={styles.golocCap}>{t('pick.goTo')}</Text>
        <Text style={styles.golocAddr}>{pick.location}</Text>
      </View>

      <Card style={styles.item}>
        <View style={styles.head}>
          <Text style={styles.sku}>{`SKU ${pick.sku}`}</Text>
          <Text style={styles.product}>{pick.product}</Text>
          <View style={styles.fefo}>
            <StatusBadge kind={pick.fefoStatus} label={pick.fefo} />
          </View>
        </View>
        <View style={styles.pickrow}>
          <Text style={styles.lbl}>{t('pick.qty')}</Text>
          <QuantityWithUnit
            value={pick.qty}
            unit={pick.unit}
            size={fs['2xl']}
            tone={theme.color.brand}
          />
        </View>
      </Card>

      <View style={styles.scanseq}>
        {steps.map((st, i) => {
          const done = i < scanned;
          const active = i === scanned;
          return (
            <View
              key={st.label}
              style={[styles.step, active && styles.stepActive, done && styles.stepDone]}
            >
              <View style={[styles.dot, done && styles.dotDone]}>
                <Text style={[styles.dotText, done && styles.dotTextDone]}>
                  {done ? '✓' : String(i + 1)}
                </Text>
              </View>
              <View style={styles.stepTx}>
                <Text style={styles.stepTitle}>{st.label}</Text>
                <Text style={styles.stepDetail}>{st.detail}</Text>
              </View>
            </View>
          );
        })}
      </View>

      <View style={styles.scanWrap}>
        <ScanField
          placeholder={allScanned ? t('pick.scanned') : prompts[scanned]}
          onScan={() => setScanned((n) => Math.min(steps.length, n + 1))}
        />
      </View>

      <ActionSheet
        visible={sheetOpen}
        title={t('pick.shortTitle')}
        options={[
          { key: 'shortAtLocation', label: t('pick.reason.shortAtLocation') },
          {
            key: 'batchBlocked',
            label: t('pick.reason.batchBlocked'),
            hint: t('pick.reason.batchBlockedHint'),
            danger: true,
          },
          { key: 'damaged', label: t('pick.reason.damaged') },
        ]}
        onSelect={(k) => void short(k as ShortReason)}
        onClose={() => setSheetOpen(false)}
      />
    </ScreenScaffold>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    prog: {
      backgroundColor: t.color.surface,
      paddingVertical: s[3],
      paddingHorizontal: s[5],
      flexDirection: 'row',
      alignItems: 'center',
      gap: s[3],
      borderBottomWidth: 1,
      borderBottomColor: t.color.line,
    },
    progLbl: { fontSize: fs.xs, color: t.color.inkFaint },
    track: {
      flex: 1,
      height: 8,
      backgroundColor: t.color.line,
      borderRadius: radius.pill,
      overflow: 'hidden',
    },
    fill: { height: '100%', backgroundColor: t.status.transit.fg },

    goloc: {
      margin: s[5],
      backgroundColor: t.status.transit.bg,
      borderWidth: 2,
      borderColor: t.status.transit.fg,
      borderRadius: radius.lg,
      paddingVertical: s[6],
      paddingHorizontal: s[5],
      alignItems: 'center',
    },
    golocCap: {
      fontSize: fs.xs,
      color: t.status.transit.fg,
      textTransform: 'uppercase',
      letterSpacing: 1,
    },
    golocAddr: {
      fontSize: fs['2xl'],
      fontWeight: '800',
      color: t.status.transit.fg,
      marginTop: s[2],
      textAlign: 'center',
    },

    item: { marginHorizontal: s[5] },
    head: { padding: s[5] },
    sku: { fontSize: fs.xs, color: t.color.inkFaint },
    product: { fontSize: fs.md, fontWeight: '700', color: t.color.ink, marginTop: 2 },
    fefo: { marginTop: s[3] },
    pickrow: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingVertical: s[4],
      paddingHorizontal: s[5],
      borderTopWidth: 1,
      borderTopColor: t.color.lineSoft,
    },
    lbl: { fontSize: fs.sm, color: t.color.inkSoft },

    scanseq: { marginHorizontal: s[5], marginBottom: s[4], gap: s[2] },
    step: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: s[3],
      backgroundColor: t.color.surface,
      borderWidth: 1,
      borderColor: t.color.line,
      borderRadius: radius.md,
      paddingVertical: s[3],
      paddingHorizontal: s[4],
    },
    stepActive: { borderColor: t.color.brand },
    stepDone: { borderColor: t.status.available.fg },
    dot: {
      width: 28,
      height: 28,
      borderRadius: 14,
      backgroundColor: t.color.canvas,
      borderWidth: 1,
      borderColor: t.color.line,
      alignItems: 'center',
      justifyContent: 'center',
    },
    dotDone: { backgroundColor: t.status.available.fg, borderColor: t.status.available.fg },
    dotText: { fontWeight: '800', fontSize: fs.sm, color: t.color.inkFaint },
    dotTextDone: { color: '#fff' },
    stepTx: { flex: 1 },
    stepTitle: { fontSize: fs.sm, fontWeight: '700', color: t.color.ink },
    stepDetail: { fontSize: fs.xs, color: t.color.inkFaint },

    scanWrap: { marginHorizontal: s[5] },
  });
