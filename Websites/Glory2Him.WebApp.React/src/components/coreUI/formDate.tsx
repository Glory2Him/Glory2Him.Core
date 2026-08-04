export interface FormDateProps {
    label?: string;
    value?: Date | null;
    onValueChange?: (value: Date | null) => void;
}

function toInputValue(value: Date | null | undefined): string {
    if (value == null) {
        return '';
    }

    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');

    return `${value.getFullYear()}-${month}-${day}`;
}

export function FormDate({ label, value = null, onValueChange }: FormDateProps) {
    return (
        <div className="mb-3">
            {label != null && label.length > 0 && (
                <label className="form-label">{label}</label>
            )}
            <input
                type="date"
                className="form-control"
                value={toInputValue(value)}
                onChange={(event) => {
                    const rawValue = event.target.value;
                    const parsedValue = rawValue.length === 0 ? NaN : Date.parse(`${rawValue}T00:00:00`);
                    onValueChange?.(Number.isNaN(parsedValue) ? null : new Date(parsedValue));
                }} />
        </div>
    );
}
