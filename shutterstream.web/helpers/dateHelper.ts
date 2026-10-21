/**
 * Formats a date a photo was taken, e.g. "14 March 2025, 15:09".
 *
 * Cameras record the time a photo was taken as wall clock time with no timezone. The API
 * serialises it with a `Z` suffix, so it must be formatted in UTC or it would shift by the
 * viewer's offset and show the wrong time (or even the wrong day).
 *
 * @param dateString - ISO date string from the API
 * @returns Formatted date and time string
 */
export const formatDateTaken = (dateString: string): string => {
  return new Date(dateString).toLocaleString('en-GB', {
    timeZone: 'UTC',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

/**
 * Formats an album date as month and year, e.g. "March 2025". Uses UTC for the same reason as `formatDateTaken`.
 *
 * @param dateString - ISO date string from the API
 * @returns Formatted month and year
 */
export const formatAlbumDate = (dateString: string): string => {
  return new Date(dateString).toLocaleDateString('en-GB', {
    timeZone: 'UTC',
    month: 'long',
    year: 'numeric'
  })
}

/**
 * Formats a real point in time (e.g. when a library scan ran) in the viewer's timezone, e.g. "14 Mar 2025, 15:09".
 *
 * @param dateString - ISO date string from the API
 * @returns Formatted date and time string
 */
export const formatTimestamp = (dateString: string): string => {
  return new Date(dateString).toLocaleString('en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}
