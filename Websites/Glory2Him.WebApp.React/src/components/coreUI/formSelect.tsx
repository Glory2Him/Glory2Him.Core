import { SelectOption } from '../../models/coreUI/selectOption';

export interface FormSelectProps {
    label?: string;
    value?: string;
    onValueChange?: (value: string) => void;
    options?: ReadonlyArray<SelectOption>;
}

export function FormSelect({ label, value = '', onValueChange, options = [] }: FormSelectProps) {
    return (
        <div className="d-inline-flex align-items-center gap-2">
            {label != null && label.length > 0 && (
                <label className="form-label mb-0 small text-nowrap">{label}</label>
            )}
            <select
                className="form-select form-select-sm"
                value={value}
                onChange={(event) => onValueChange?.(event.target.value)}>
                {options.map((option) => (
                    <option key={option.value} value={option.value}>{option.text}</option>
                ))}
            </select>
        </div>
    );
}
