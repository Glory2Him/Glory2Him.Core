import { useId } from 'react';

export interface FormSwitchProps {
    label?: string;
    value?: boolean;

    // A switch that shows a setting without offering to change it. The read-only face of the
    // content item settings panel needs the SAME rows as its modify face — a reader comparing
    // the two must see one layout, not two — so the difference between them is this flag rather
    // than a second way of drawing a boolean.
    disabled?: boolean;

    onValueChange?: (value: boolean) => void;
}

export function FormSwitch({
    label,
    value = false,
    disabled = false,
    onValueChange
}: FormSwitchProps) {
    // TIED TO ITS OWN LABEL. Without the pairing the switch has no accessible name at all —
    // a reader hears "checkbox, not checked" and is told nothing about what it governs. The id
    // is generated rather than taken as a prop so no caller has to invent unique ones for the
    // nineteen switches a settings panel puts on one screen.
    const switchId = useId();

    return (
        <div className="form-check form-switch mb-3">
            <input
                id={switchId}
                className="form-check-input"
                type="checkbox"
                checked={value}
                disabled={disabled}
                onChange={(event) => onValueChange?.(event.target.checked)} />
            {label != null && label.length > 0 && (
                <label className="form-check-label" htmlFor={switchId}>{label}</label>
            )}
        </div>
    );
}
