import { router, useFocusEffect } from 'expo-router';
import { useCallback, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { BarControls, BottomNav, Icon, ResourceView, ScanField } from '@/shared/ui';
import { useResource } from '@/core/api/useResource';
import { fs, radius, s, shadow } from '@/shared/theme/tokens';
import { useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';
import { useAuth } from '@/shared/auth/AuthContext';
import { getTasks, type TaskTile } from './tasks.model';

/** Terminal — Task hub (terminal-1-hub). The operator's landing screen. */
export function TaskHubScreen() {
  const tasks = useResource(getTasks);

  // Re-read the piles whenever the hub regains focus, so a task confirmed in a
  // child screen shows its pile dropping when the operator returns (the
  // terminal's analog of the admin's query invalidation after a mutation).
  const reloadTasks = tasks.reload;
  useFocusEffect(useCallback(() => reloadTasks(), [reloadTasks]));

  return <ResourceView resource={tasks}>{(tk) => <TaskHubView tasks={tk} />}</ResourceView>;
}

function TaskHubView({ tasks }: { tasks: TaskTile[] }) {
  const insets = useSafeAreaInsets();
  const styles = useThemedStyles(makeStyles);
  const t = useT();
  // Identity comes from the signed-in operator (badge sign-in) — the terminal has
  // no separate `operator/me` read; the session is the source of truth.
  const { operator: account, signOut } = useAuth();

  // The floor has RF dead spots — tap the chip to simulate a signal drop.
  // Offline, confirmations queue on-device and sync when signal returns.
  const [online, setOnline] = useState(true);
  const queued = 3;

  return (
    <View style={styles.root}>
      {/* Identity / connectivity bar */}
      <View style={[styles.bar, { paddingTop: insets.top + s[4] }]}>
        <View style={styles.barLeft}>
          <Text style={styles.who}>{account?.name}</Text>
          <Text style={styles.site}>{account?.site}</Text>
        </View>
        <View style={styles.barRight}>
          <BarControls />
          <Pressable
            style={[styles.net, !online && styles.netOff]}
            onPress={() => setOnline((v) => !v)}
            accessibilityRole="button"
            accessibilityLabel={t('a11y.toggleConnectivity')}
          >
            <Text style={styles.netText}>● {online ? t('common.online') : t('common.offline')}</Text>
          </Pressable>
          <Pressable
            style={styles.logout}
            onPress={signOut}
            accessibilityRole="button"
            accessibilityLabel={t('a11y.signOut')}
            hitSlop={8}
          >
            <Icon name="logout" size={24} color="#fff" />
          </Pressable>
        </View>
      </View>

      {!online && (
        <View style={styles.sync}>
          <Text style={styles.syncText}>{t('hub.offline')}</Text>
          <View style={styles.syncQ}>
            <Text style={styles.syncQText}>{t('hub.queued', { n: queued })}</Text>
          </View>
        </View>
      )}

      <ScrollView contentContainerStyle={styles.scroll}>
        <View style={styles.scanWrap}>
          <ScanField placeholder={t('hub.scanStart')} />
        </View>

        <Text style={styles.h1}>{t('hub.tasksHeading')}</Text>
        <View style={styles.tasks}>
          {tasks.map((task) => (
            <Pressable
              key={task.kind}
              style={({ pressed }) => [styles.task, { backgroundColor: task.color }, pressed && styles.pressed]}
              onPress={() => router.push(task.route)}
              accessibilityRole="button"
            >
              <View style={styles.taskIcon}>
                <Icon name={task.icon} size={40} color="#fff" />
              </View>
              <View style={styles.taskTxt}>
                <Text style={styles.taskTitle}>{t(`tasks.${task.kind}`)}</Text>
                <Text style={styles.taskDetail}>{task.detail}</Text>
              </View>
              <Text style={styles.taskCount}>{task.count}</Text>
            </Pressable>
          ))}
        </View>
      </ScrollView>

      <View style={{ paddingBottom: insets.bottom }}>
        <BottomNav active="tasks" />
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
    barLeft: { flex: 1 },
    barRight: { flexDirection: 'row', alignItems: 'center', gap: s[3] },
    who: { color: '#fff', fontSize: fs.md, fontWeight: '700' },
    site: { color: '#fff', opacity: 0.85, fontSize: fs.xs, marginTop: 2 },
    net: { backgroundColor: 'rgba(255,255,255,0.18)', paddingVertical: 4, paddingHorizontal: 10, borderRadius: radius.pill },
    netOff: { backgroundColor: t.status.transit.fg },
    netText: { color: '#fff', fontSize: fs.xs },
    logout: {
      width: 40,
      height: 40,
      borderRadius: radius.pill,
      backgroundColor: 'rgba(255,255,255,0.18)',
      alignItems: 'center',
      justifyContent: 'center',
    },

    sync: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: s[3],
      backgroundColor: t.status.transit.bg,
      paddingVertical: s[3],
      paddingHorizontal: s[5],
      borderBottomWidth: 1,
      borderBottomColor: t.status.transit.fg,
    },
    syncText: { flex: 1, color: t.status.transit.fg, fontSize: fs.sm, fontWeight: '600' },
    syncQ: { backgroundColor: t.status.transit.fg, borderRadius: radius.pill, paddingVertical: 2, paddingHorizontal: 10 },
    syncQText: { color: '#fff', fontSize: fs.xs, fontWeight: '700' },

    scroll: { paddingBottom: s[5] },
    scanWrap: { margin: s[5] },

    h1: {
      fontSize: fs.sm,
      color: t.color.inkFaint,
      textTransform: 'uppercase',
      letterSpacing: 1,
      marginHorizontal: s[5],
      marginBottom: s[2],
    },
    tasks: { paddingHorizontal: s[5], gap: s[4] },
    task: {
      minHeight: 96,
      borderRadius: radius.lg,
      padding: s[5],
      flexDirection: 'row',
      alignItems: 'center',
      gap: s[5],
      ...shadow.e2,
    },
    pressed: { opacity: 0.9 },
    taskIcon: { width: 48, alignItems: 'center' },
    taskTxt: { flex: 1 },
    taskTitle: { fontSize: fs.lg, fontWeight: '700', color: '#fff' },
    taskDetail: { fontSize: fs.sm, color: '#fff', opacity: 0.9, marginTop: 2 },
    taskCount: { fontSize: fs.xl, fontWeight: '800', color: '#fff' },
  });
