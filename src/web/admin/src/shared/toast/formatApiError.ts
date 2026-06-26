import i18n from '@/shared/i18n/i18n';
import { ApiError } from '@/core/api/client';

/**
 * Turn a thrown request error into a user-facing, localized message.
 *
 * When the API supplies a stable domain error code (`DomainException`, echoed by
 * the seam as `ApiError.code`), we render the matching `errors.<code>` string —
 * the same catalogue the backend raises, in the UI's language. Fallbacks, in
 * order: the API's own message, then a generic string. Unknown codes therefore
 * still show *something* useful without needing a translation for every code.
 */
export function formatApiError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.code) {
      return i18n.t(`errors.${error.code}`, {
        defaultValue: error.message || i18n.t('errors.generic'),
      });
    }
    return error.message || i18n.t('errors.generic');
  }
  if (error instanceof Error && error.message) return error.message;
  return i18n.t('errors.generic');
}
