export interface FormSwitchProps {
    label?: string;
    value?: boolean;
    onValueChange?: (value: boolean) => void;
}

export function FormSwitch({ label, value = false, onValueChange }: FormSwitchProps) {
    return (
        <div className="form-check form-switch mb-3">
            <input
                className="form-check-input"
                type="checkbox"
                checked={value}
                onChange={(event) => onValueChange?.(event.target.checked)} />
            {label != null && label.length > 0 && (
                <label className="form-check-label">{label}</label>
            )}
        </div>
    );
}
