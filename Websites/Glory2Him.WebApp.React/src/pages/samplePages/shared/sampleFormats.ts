// Mirrors the C# "MMMM dd, yyyy" format the post-single demos used, so the rendered text
// stays pixel-identical (full month name, zero-padded day).

const fullMonthNames = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December',
];

export function formatLongDate(date: Date): string {
    const month = fullMonthNames[date.getMonth()];
    const day = String(date.getDate()).padStart(2, '0');

    return `${month} ${day}, ${date.getFullYear()}`;
}
