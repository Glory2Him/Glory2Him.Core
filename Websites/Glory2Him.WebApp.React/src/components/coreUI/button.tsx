import { ReactNode } from 'react';

export interface ButtonProps {
    color?: string;
    type?: 'button' | 'submit' | 'reset';
    disabled?: boolean;
    cssClass?: string;
    children?: ReactNode;
    onClick?: () => void;
}

export function Button({
    color = 'primary',
    type = 'button',
    disabled = false,
    cssClass = '',
    children,
    onClick,
}: ButtonProps) {
    return (
        <button
            type={type}
            className={`btn btn-${color} ${cssClass}`}
            disabled={disabled}
            onClick={onClick}>
            {children}
        </button>
    );
}
