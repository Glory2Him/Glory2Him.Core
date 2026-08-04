export interface FormTextProps {
    label?: string;
    value?: string;
    onValueChange?: (value: string) => void;
    placeholder?: string;
}

export function FormText({ label, value, onValueChange, placeholder }: FormTextProps) {
    return (
        <div className="mb-3">
            {label != null && label.length > 0 && (
                <label className="form-label">{label}</label>
            )}
            <input
                className="form-control"
                // Left uncontrolled when the caller supplies no value, matching the Blazor
                // component's decorative use inside CommentThread's reply form.
                {...(value !== undefined ? { value } : {})}
                placeholder={placeholder}
                onChange={(event) => onValueChange?.(event.target.value)} />
        </div>
    );
}
