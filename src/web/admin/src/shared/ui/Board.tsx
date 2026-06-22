import { Children, type ReactNode } from 'react';

import styles from './Board.module.css';

/**
 * Kanban board layout — a row of status columns, each a stack of cards. The
 * column count is derived from the children so the grid stays even. The cards
 * themselves are feature-specific; this owns only the board/column chrome.
 */
export function Board({ children }: { children: ReactNode }) {
  const count = Children.count(children);
  return (
    <div
      className={styles.board}
      style={{ gridTemplateColumns: `repeat(${count}, minmax(0, 1fr))` }}
    >
      {children}
    </div>
  );
}

export function BoardColumn({
  title,
  count,
  children,
}: {
  title: string;
  count: number;
  children: ReactNode;
}) {
  return (
    <div className={styles.col}>
      <h3 className={styles.head}>
        <span>{title}</span>
        <span className={styles.count}>{count}</span>
      </h3>
      <div className={styles.stack}>{children}</div>
    </div>
  );
}
