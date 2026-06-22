import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown, ChevronsUpDown, ChevronUp } from 'lucide-react';
import {
  flexRender,
  getCoreRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  useReactTable,
  type ColumnDef,
  type RowData,
  type SortingState,
} from '@tanstack/react-table';

import styles from './DataTable.module.css';

// Column meta lets a column opt into right-aligned tabular numerals.
declare module '@tanstack/react-table' {
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  interface ColumnMeta<TData extends RowData, TValue> {
    align?: 'right';
  }
}

/**
 * Generic headless table (TanStack Table) rendered with the design-system
 * table classes. Sortable (click a header) and paginated (footer with a row
 * count), so every table scales to real volumes. Headless on purpose: the
 * markup inherits tokens.css instead of fighting a styled grid's opinions.
 */
export function DataTable<T>({
  columns,
  data,
  onRowClick,
  pageSize = 25,
}: {
  columns: ColumnDef<T, unknown>[];
  data: T[];
  /** When set, rows become clickable (drill into a detail) — keyboard-accessible. */
  onRowClick?: (row: T) => void;
  pageSize?: number;
}) {
  const { t } = useTranslation();
  const [sorting, setSorting] = useState<SortingState>([]);

  const table = useReactTable({
    data,
    columns,
    state: { sorting },
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    initialState: { pagination: { pageSize } },
  });

  const total = table.getRowCount();
  const { pageIndex, pageSize: ps } = table.getState().pagination;
  const start = total === 0 ? 0 : pageIndex * ps + 1;
  const end = Math.min(start + ps - 1, total);

  return (
    <>
      <div className={styles.scroll}>
        <table className={styles.table}>
          <thead>
          {table.getHeaderGroups().map((hg) => (
            <tr key={hg.id}>
              {hg.headers.map((header) => {
                const sortable = header.column.getCanSort();
                const dir = header.column.getIsSorted();
                return (
                  <th
                    key={header.id}
                    className={`${header.column.columnDef.meta?.align === 'right' ? styles.num : ''} ${
                      sortable ? styles.sortable : ''
                    }`}
                    onClick={sortable ? header.column.getToggleSortingHandler() : undefined}
                  >
                    <span className={styles.headInner}>
                      {header.isPlaceholder
                        ? null
                        : flexRender(header.column.columnDef.header, header.getContext())}
                      {sortable ? (
                        <span className={styles.sortIcon} aria-hidden>
                          {dir === 'asc' ? (
                            <ChevronUp size={13} />
                          ) : dir === 'desc' ? (
                            <ChevronDown size={13} />
                          ) : (
                            <ChevronsUpDown size={13} className={styles.sortIdle} />
                          )}
                        </span>
                      ) : null}
                    </span>
                  </th>
                );
              })}
            </tr>
          ))}
        </thead>
          <tbody>
            {table.getRowModel().rows.map((row) => (
              <tr
                key={row.id}
                className={onRowClick ? styles.clickable : undefined}
                onClick={onRowClick ? () => onRowClick(row.original) : undefined}
                role={onRowClick ? 'button' : undefined}
                tabIndex={onRowClick ? 0 : undefined}
                onKeyDown={
                  onRowClick
                    ? (e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault();
                          onRowClick(row.original);
                        }
                      }
                    : undefined
                }
              >
                {row.getVisibleCells().map((cell) => (
                  <td
                    key={cell.id}
                    className={cell.column.columnDef.meta?.align === 'right' ? styles.num : undefined}
                  >
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {total > ps ? (
        <div className={styles.footer}>
          <span className={styles.count}>{t('table.range', { start, end, total })}</span>
          <div className={styles.pageBtns}>
            <button
              type="button"
              className={styles.pageBtn}
              disabled={!table.getCanPreviousPage()}
              onClick={() => table.previousPage()}
            >
              {t('table.prev')}
            </button>
            <button
              type="button"
              className={styles.pageBtn}
              disabled={!table.getCanNextPage()}
              onClick={() => table.nextPage()}
            >
              {t('table.next')}
            </button>
          </div>
        </div>
      ) : null}
    </>
  );
}
