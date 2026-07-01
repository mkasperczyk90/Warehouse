import { router } from 'expo-router';
import { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';

import {
  BigActionButton,
  Card,
  CheckRow,
  Chip,
  ResourceView,
  ScanField,
  ScreenScaffold,
} from '@/shared/ui';
import { useResource } from '@/core/api/useResource';
import { fs, radius, s } from '@/shared/theme/tokens';
import { useTheme, useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';
import {
  confirmPutAway,
  getPutAwayTask,
  proposeAnotherBay,
  type PutAwayTask,
} from './putaway.model';

/** Terminal — Put-away (terminal-3-putaway · UC-04). */
export function PutAwayScreen() {
  const task = useResource(getPutAwayTask);
  return (
    <ResourceView resource={task}>
      {(data) => <PutAwayView task={data} reload={task.reload} />}
    </ResourceView>
  );
}

function PutAwayView({ task, reload }: { task: PutAwayTask; reload: () => void }) {
  const theme = useTheme();
  const t = useT();
  const styles = useThemedStyles(makeStyles);
  const [pending, setPending] = useState(false);

  async function confirm() {
    setPending(true);
    try {
      await confirmPutAway();
      router.back();
    } catch {
      setPending(false);
    }
  }

  // Location full / over capacity → the system proposes the next legal bay (it
  // never suggests an incompatible one — Invariant #1/#2). We refetch in place.
  async function proposeAnother() {
    setPending(true);
    try {
      await proposeAnotherBay();
      reload();
    } finally {
      setPending(false);
    }
  }

  return (
    <ScreenScaffold
      title={t('putaway.title')}
      subtitle={t('putaway.task', { n: task.task, total: task.ofTasks })}
      actions={
        <>
          <BigActionButton
            label={t('putaway.confirm')}
            accent={theme.status.available.fg}
            disabled={pending}
            onPress={confirm}
          />
          <BigActionButton
            label={t('putaway.full')}
            kind="ghost"
            disabled={pending}
            onPress={proposeAnother}
          />
        </>
      }
    >
      <Card style={styles.pallet}>
        <Text style={styles.lpn}>{t('putaway.lpn', { lpn: task.lpn })}</Text>
        <Text style={styles.product}>{task.product}</Text>
        <View style={styles.chips}>
          {task.chips.map((c) => (
            <Chip key={c} label={c} />
          ))}
          <Chip label={task.coldChip} cold />
        </View>
      </Card>

      <View style={styles.propose}>
        <Text style={styles.cap}>{t('putaway.proposed')}</Text>
        <View style={styles.loc}>
          <Text style={styles.addr}>{task.location}</Text>
          <Text style={styles.why}>{task.why}</Text>
        </View>
      </View>

      <View style={styles.checks}>
        {task.checks.map((c) => (
          <CheckRow key={c} label={c} />
        ))}
      </View>

      <View style={styles.scanWrap}>
        <ScanField placeholder={t('putaway.scan')} dashed />
      </View>
    </ScreenScaffold>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    pallet: { margin: s[5], padding: s[5] },
    lpn: { fontSize: fs.xs, color: t.color.inkFaint },
    product: {
      fontSize: fs.md,
      fontWeight: '700',
      color: t.color.ink,
      marginTop: 2,
      marginBottom: s[3],
    },
    chips: { flexDirection: 'row', flexWrap: 'wrap', gap: s[2] },

    propose: { marginHorizontal: s[5] },
    cap: {
      fontSize: fs.xs,
      color: t.color.inkFaint,
      textTransform: 'uppercase',
      letterSpacing: 1,
      marginBottom: s[2],
    },
    loc: {
      backgroundColor: t.status.available.bg,
      borderWidth: 2,
      borderColor: t.status.available.fg,
      borderRadius: radius.lg,
      paddingVertical: s[6],
      paddingHorizontal: s[5],
      alignItems: 'center',
    },
    addr: {
      fontSize: fs['2xl'],
      fontWeight: '800',
      color: t.status.available.fg,
      letterSpacing: 0.5,
      textAlign: 'center',
    },
    why: { fontSize: fs.sm, color: t.color.inkSoft, marginTop: s[2], textAlign: 'center' },

    checks: { margin: s[5], gap: s[2] },
    scanWrap: { marginHorizontal: s[5] },
  });
