import { router } from 'expo-router';
import { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';

import {
  ActionSheet,
  BigActionButton,
  Card,
  Keypad,
  QuantityWithUnit,
  ResourceView,
  ScanField,
  ScreenScaffold,
  Stepper,
} from '@/shared/ui';
import { useResource } from '@/core/api/useResource';
import { fs, radius, s } from '@/shared/theme/tokens';
import { useTheme, useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';
import {
  confirmReceipt,
  getReceipt,
  type DiscrepancyReason,
  type Receipt,
} from './receiving.model';

/** Terminal — Goods receipt (terminal-2-receive · UC-02). */
export function ReceiveScreen() {
  const receipt = useResource(getReceipt);
  return <ResourceView resource={receipt}>{(data) => <ReceiveView receipt={data} />}</ResourceView>;
}

function ReceiveView({ receipt }: { receipt: Receipt }) {
  const theme = useTheme();
  const t = useT();
  const styles = useThemedStyles(makeStyles);
  const [counted, setCounted] = useState(receipt.expectedQty);
  const [keypadOpen, setKeypadOpen] = useState(false);
  const [sheetOpen, setSheetOpen] = useState(false);
  const [pending, setPending] = useState(false);

  // Confirm the line (optionally with a discrepancy reason); the receipt always
  // proceeds — a discrepancy is recorded, not a blocker (exceptions §Inbound).
  async function confirm(reason?: DiscrepancyReason) {
    setPending(true);
    try {
      await confirmReceipt({ counted, reason });
      router.back();
    } catch {
      setPending(false); // stay on the line so the operator can retry
    }
  }

  return (
    <ScreenScaffold
      title={t('receive.title')}
      subtitle={t('receive.line', { n: receipt.line, total: receipt.ofLines })}
      actions={
        <>
          <BigActionButton
            label={t('receive.confirm')}
            accent={theme.status.available.fg}
            disabled={pending}
            onPress={() => confirm()}
          />
          {/* a shortage/discrepancy is routine, not danger — neutral, not red */}
          <BigActionButton
            label={t('receive.discrepancy')}
            kind="ghost"
            disabled={pending}
            onPress={() => setSheetOpen(true)}
          />
        </>
      }
    >
      {/* ASN context banner */}
      <View style={styles.ctx}>
        <Text style={styles.ctxText}>{`${receipt.asn} · ${receipt.supplier}`}</Text>
        <Text style={styles.ctxText}>{receipt.dock}</Text>
      </View>

      <View style={styles.scanWrap}>
        <ScanField placeholder={t('receive.scan')} variant="available" />
      </View>

      <Card style={styles.card}>
        <View style={styles.head}>
          <Text style={styles.sku}>{t('receive.skuScanned', { sku: receipt.sku })}</Text>
          <Text style={styles.product}>{receipt.product}</Text>
        </View>

        <View style={styles.expected}>
          <Text style={styles.expectedLbl}>{t('receive.expected')}</Text>
          <Text style={styles.expectedLbl}>
            <QuantityWithUnit
              value={receipt.expectedQty}
              unit={receipt.unit}
              size={fs.sm}
              tone={theme.status.transit.fg}
            />
            {`  ${receipt.expectedNote}`}
          </Text>
        </View>

        <View style={styles.row}>
          <Text style={styles.lbl}>{t('receive.counted')}</Text>
          <Stepper value={counted} onChange={setCounted} onPressValue={() => setKeypadOpen(true)} />
        </View>
        <Text style={styles.keyHint}>{t('receive.keyHint')}</Text>

        <View style={styles.batch}>
          <View style={styles.field}>
            <Text style={styles.fieldLbl}>{t('receive.batch')}</Text>
            <View style={styles.fieldBox}>
              <Text style={styles.fieldVal}>{receipt.batch}</Text>
            </View>
          </View>
          <View style={styles.field}>
            <Text style={styles.fieldLbl}>{t('receive.bbe')}</Text>
            <View style={styles.fieldBox}>
              <Text style={styles.fieldVal}>{receipt.bestBefore}</Text>
            </View>
          </View>
        </View>
      </Card>

      <Keypad
        visible={keypadOpen}
        value={counted}
        expected={receipt.expectedQty}
        unit={receipt.unit}
        label={t('receive.keypadLabel', { product: receipt.product, unit: receipt.unit })}
        onConfirm={(n) => {
          setCounted(n);
          setKeypadOpen(false);
        }}
        onClose={() => setKeypadOpen(false)}
      />

      <ActionSheet
        visible={sheetOpen}
        title={t('receive.discrepancyTitle')}
        options={[
          { key: 'shortage', label: t('receive.reason.shortage') },
          { key: 'overage', label: t('receive.reason.overage') },
          {
            key: 'damage',
            label: t('receive.reason.damage'),
            hint: t('receive.reason.damageHint'),
            danger: true,
          },
        ]}
        onSelect={(k) => {
          setSheetOpen(false);
          void confirm(k as DiscrepancyReason);
        }}
        onClose={() => setSheetOpen(false)}
      />
    </ScreenScaffold>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    ctx: {
      backgroundColor: t.status.reserved.bg,
      paddingVertical: s[3],
      paddingHorizontal: s[5],
      flexDirection: 'row',
      justifyContent: 'space-between',
    },
    ctxText: { color: t.status.reserved.fg, fontSize: fs.sm },

    scanWrap: { margin: s[5] },
    card: { marginHorizontal: s[5] },

    head: { padding: s[5], borderBottomWidth: 1, borderBottomColor: t.color.lineSoft },
    sku: { fontSize: fs.xs, color: t.color.inkFaint },
    product: { fontSize: fs.md, fontWeight: '700', color: t.color.ink, marginTop: 2 },

    expected: {
      backgroundColor: t.status.transit.bg,
      paddingVertical: s[3],
      paddingHorizontal: s[5],
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
    },
    expectedLbl: { fontSize: fs.xs, color: t.status.transit.fg },

    row: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingVertical: s[4],
      paddingHorizontal: s[5],
    },
    lbl: { fontSize: fs.sm, color: t.color.inkSoft },
    keyHint: {
      fontSize: fs.xs,
      color: t.color.inkFaint,
      textAlign: 'right',
      paddingHorizontal: s[5],
      paddingBottom: s[3],
    },

    batch: {
      flexDirection: 'row',
      gap: s[3],
      padding: s[4],
      paddingHorizontal: s[5],
      borderTopWidth: 1,
      borderTopColor: t.color.lineSoft,
    },
    field: { flex: 1 },
    fieldLbl: { fontSize: fs.xs, color: t.color.inkFaint, marginBottom: 4 },
    fieldBox: { borderWidth: 1, borderColor: t.color.line, borderRadius: radius.sm, padding: s[3] },
    fieldVal: { fontSize: fs.sm, fontWeight: '600', color: t.color.ink },
  });
