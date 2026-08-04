// Mirrors the C# format strings the Blazor CoreUI components used, so the rendered
// text stays pixel-identical: "MMM dd, yyyy" and "MMM dd, yyyy 'at' h:mm tt".

const monthNames = [
    'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
    'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
];

export function formatDate(date: Date): string {
    const month = monthNames[date.getMonth()];
    const day = String(date.getDate()).padStart(2, '0');

    return `${month} ${day}, ${date.getFullYear()}`;
}

export function formatDateTime(date: Date): string {
    const hours24 = date.getHours();
    const hours12 = hours24 % 12 === 0 ? 12 : hours24 % 12;
    const minutes = String(date.getMinutes()).padStart(2, '0');
    const meridiem = hours24 < 12 ? 'AM' : 'PM';

    return `${formatDate(date)} at ${hours12}:${minutes} ${meridiem}`;
}
