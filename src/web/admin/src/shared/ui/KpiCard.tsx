import styles from './KpiCard.module.css';

/** KPI card for the dashboard strip. `warn` paints the value in blocked-red. */
export function KpiCard({
  label,
  value,
  unit,
  warn = false,
}: {
  label: string;
  value: number;
  unit?: string;
  warn?: boolean;
}) {
  return (
    <div className={`${styles.kpi} ${warn ? styles.warn : ''}`}>
      <div className={styles.label}>{label}</div>
      <div className={styles.value}>
        {value.toLocaleString()}
        {unit ? <small>{unit}</small> : null}
      </div>
    </div>
  );
}
