import { router } from 'expo-router';
import { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';

import { BigActionButton, Card, CheckRow, Chip, ResourceView, ScanField, ScreenScaffold, Stepper } from '@/shared/ui';
import { useResource } from '@/core/api/useResource';
import { fs, s } from '@/shared/theme/tokens';
import { useTheme, useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';
import { confirmMove, getMoveTask, transferMove, type MoveTask } from './movement.model';

/** Terminal — Move stock (terminal-5-move · UC-06). The violet flow. */
export function MovementScreen() {
  const move = useResource(getMoveTask);
  return <ResourceView resource={move}>{(data) => <MovementView move={data} />}</ResourceView>;
}

function MovementView({ move }: { move: MoveTask }) {
  const theme = useTheme();
  const t = useT();
  const styles = useThemedStyles(makeStyles);
  const [qty, setQty] = useState(move.qty);
  const [pending, setPending] = useState(false);

  // Same write, two destinations: a local move, or an inter-warehouse transfer
  // that leaves the goods InTransit until the destination receipt (UC-06).
  async function commit(kind: 'move' | 'transfer') {
    setPending(true);
    try {
      await (kind === 'move' ? confirmMove(qty) : transferMove(qty));
      router.back();
    } catch {
      setPending(false);
    }
  }

  return (
    <ScreenScaffold
      title={t('move.title')}
      subtitle={t('move.task', { n: move.task, total: move.ofTasks })}
      accent={theme.color.move}
      actions={
        <>
          <BigActionButton label={t('move.confirm')} accent={theme.color.move} disabled={pending} onPress={() => commit('move')} />
          <BigActionButton label={t('move.transfer')} kind="ghost" accent={theme.status.transit.fg} disabled={pending} onPress={() => commit('transfer')} />
        </>
      }
    >
      {/* From → To legs */}
      <View style={styles.flow}>
        <Card style={[styles.leg, styles.legFrom]}>
          <Text style={styles.cap}>{t('move.from')}</Text>
          <Text style={styles.addr}>{move.from}</Text>
        </Card>
        <Text style={styles.arrow}>↓</Text>
        <Card style={[styles.leg, styles.legTo]}>
          <Text style={styles.cap}>{t('move.to')}</Text>
          <Text style={[styles.addr, styles.addrTo]}>{move.to}</Text>
        </Card>
      </View>

      <Card style={styles.item}>
        <Text style={styles.sku}>{t('move.skuBatch', { sku: move.sku, batch: move.batch })}</Text>
        <Text style={styles.product}>{move.product}</Text>
        <View style={styles.chips}>
          <Chip label={move.coldChip} cold />
          <Chip label={move.bbeChip} />
        </View>
        <View style={styles.qtyrow}>
          <Text style={styles.lbl}>{t('move.qty')}</Text>
          <Stepper value={qty} onChange={setQty} />
        </View>
      </Card>

      <View style={styles.checks}>
        {move.checks.map((c) => (
          <CheckRow key={c} label={c} />
        ))}
      </View>

      <View style={styles.scanWrap}>
        <ScanField placeholder={t('move.scan')} variant="move" />
      </View>
    </ScreenScaffold>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    flow: { margin: s[5], gap: s[3] },
    leg: { paddingVertical: s[4], paddingHorizontal: s[5] },
    legFrom: { borderLeftWidth: 4, borderLeftColor: t.status.transit.fg },
    legTo: { borderLeftWidth: 4, borderLeftColor: t.status.available.fg },
    cap: { fontSize: fs.xs, color: t.color.inkFaint, textTransform: 'uppercase', letterSpacing: 1 },
    addr: { fontSize: fs.xl, fontWeight: '800', color: t.color.ink, marginTop: 2 },
    addrTo: { color: t.color.inkFaint, fontWeight: '600' },
    arrow: { textAlign: 'center', fontSize: 28, color: t.color.inkFaint },

    item: { marginHorizontal: s[5], padding: s[5] },
    sku: { fontSize: fs.xs, color: t.color.inkFaint },
    product: { fontSize: fs.md, fontWeight: '700', color: t.color.ink, marginTop: 2, marginBottom: s[3] },
    chips: { flexDirection: 'row', gap: s[2] },
    qtyrow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginTop: s[4] },
    lbl: { fontSize: fs.sm, color: t.color.inkSoft },

    checks: { margin: s[5], gap: s[2] },
    scanWrap: { marginHorizontal: s[5] },
  });
