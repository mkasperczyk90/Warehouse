import { useMemo, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import {
  Card,
  Icon,
  QuantityWithUnit,
  ResourceView,
  SearchField,
  StatusBadge,
  TabScaffold,
} from '@/shared/ui';
import { useResource } from '@/core/api/useResource';
import { fs, radius, s } from '@/shared/theme/tokens';
import { useTheme, useThemedStyles, type Theme } from '@/shared/theme/theme';
import { useT } from '@/shared/i18n/i18n';
import {
  getLookupIndex,
  KIND_FILTERS,
  searchLookup,
  type LookupKind,
  type LookupRow,
} from './lookup.model';

/** Terminal — Look up: read-only inquiry over products, locations and batches. */
export function LookupScreen() {
  const index = useResource(getLookupIndex);
  return <ResourceView resource={index}>{(data) => <LookupView index={data} />}</ResourceView>;
}

function LookupView({ index }: { index: LookupRow[] }) {
  const theme = useTheme();
  const t = useT();
  const styles = useThemedStyles(makeStyles);
  const [query, setQuery] = useState('');
  const [kind, setKind] = useState<LookupKind | 'all'>('all');

  const results = useMemo(() => searchLookup(index, query, kind), [index, query, kind]);

  return (
    <TabScaffold title={t('lookup.title')} subtitle={t('lookup.subtitle')} active="lookup">
      <View style={styles.searchWrap}>
        <SearchField value={query} onChangeText={setQuery} placeholder={t('lookup.placeholder')} />
      </View>

      <View style={styles.filters}>
        {KIND_FILTERS.map((f) => {
          const on = f.key === kind;
          return (
            <Pressable
              key={f.key}
              style={[styles.filter, on && styles.filterOn]}
              onPress={() => setKind(f.key)}
              accessibilityRole="button"
              accessibilityState={{ selected: on }}
            >
              <Text style={[styles.filterLabel, on && styles.filterLabelOn]}>
                {t(`lookup.filter.${f.key}`)}
              </Text>
            </Pressable>
          );
        })}
      </View>

      <ScrollView contentContainerStyle={styles.scroll} keyboardShouldPersistTaps="handled">
        <Text style={styles.count}>{t('lookup.count', { n: results.length })}</Text>

        {results.length === 0 ? (
          <Text style={styles.empty}>{t('lookup.empty')}</Text>
        ) : (
          <Card style={styles.list}>
            {results.map((r, i) => (
              <View key={r.id} style={[styles.row, i < results.length - 1 && styles.rowBorder]}>
                <View style={styles.rowIcon}>
                  <Icon name={r.icon} size={26} color={theme.color.inkFaint} />
                </View>
                <View style={styles.rowText}>
                  <Text style={styles.rowTitle}>{r.title}</Text>
                  <Text style={styles.rowMeta}>{r.meta}</Text>
                  {r.status && (
                    <View style={styles.badgeRow}>
                      <StatusBadge kind={r.status.kind} label={t(`status.${r.status.kind}`)} />
                    </View>
                  )}
                </View>
                {r.qty && <QuantityWithUnit value={r.qty.value} unit={r.qty.unit} size={fs.lg} />}
              </View>
            ))}
          </Card>
        )}
      </ScrollView>
    </TabScaffold>
  );
}

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    searchWrap: { margin: s[5], marginBottom: s[3] },

    filters: { flexDirection: 'row', gap: s[2], paddingHorizontal: s[5], marginBottom: s[2] },
    filter: {
      paddingVertical: s[2],
      paddingHorizontal: s[4],
      borderRadius: radius.pill,
      borderWidth: 1,
      borderColor: t.color.line,
      backgroundColor: t.color.surface,
    },
    filterOn: { backgroundColor: t.color.brand, borderColor: t.color.brand },
    filterLabel: { fontSize: fs.sm, color: t.color.inkSoft, fontWeight: '600' },
    filterLabelOn: { color: '#fff' },

    scroll: { paddingBottom: s[5] },
    count: {
      fontSize: fs.xs,
      color: t.color.inkFaint,
      marginHorizontal: s[5],
      marginVertical: s[2],
    },
    empty: { fontSize: fs.sm, color: t.color.inkFaint, margin: s[5] },

    list: { marginHorizontal: s[5] },
    row: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: s[4],
      paddingVertical: s[4],
      paddingHorizontal: s[5],
    },
    rowBorder: { borderBottomWidth: 1, borderBottomColor: t.color.lineSoft },
    rowIcon: { width: 32, alignItems: 'center' },
    rowText: { flex: 1 },
    rowTitle: { fontSize: fs.sm, fontWeight: '700', color: t.color.ink },
    rowMeta: { fontSize: fs.xs, color: t.color.inkFaint, marginTop: 2 },
    badgeRow: { marginTop: s[2] },
  });
