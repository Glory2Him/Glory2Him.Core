import { ReactNode } from 'react';

export interface CardProps {
    title?: string;
    cssClass?: string;
    headerContent?: ReactNode;
    children?: ReactNode;
    footerContent?: ReactNode;
}

export function Card({ title, cssClass = '', headerContent, children, footerContent }: CardProps) {
    return (
        <div className={`card ${cssClass}`}>
            {(title != null || headerContent != null) && (
                <div className="card-header">
                    {title != null && <span className="fw-semibold">{title}</span>}
                    {headerContent}
                </div>
            )}
            <div className="card-body">
                {children}
            </div>
            {footerContent != null && (
                <div className="card-footer">
                    {footerContent}
                </div>
            )}
        </div>
    );
}
