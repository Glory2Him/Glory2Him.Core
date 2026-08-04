export interface SpinnerProps {
    visible?: boolean;
    label?: string;
}

export function Spinner({ visible = true, label = 'Loading...' }: SpinnerProps) {
    if (!visible) {
        return null;
    }

    return (
        <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">{label}</span>
        </div>
    );
}
