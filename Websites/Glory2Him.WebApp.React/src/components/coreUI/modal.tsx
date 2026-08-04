import { ReactNode } from 'react';

// Bootstrap 5's JS is loaded globally, but the modal is driven by pure React state
// toggling the same Bootstrap classes/markup (modal fade show + backdrop) — that keeps
// visibility deterministic and testable instead of reaching into window.bootstrap.
export interface ModalProps {
    title?: string;
    visible: boolean;
    onClose?: () => void;
    children?: ReactNode;
    footerContent?: ReactNode;

    // Optional Bootstrap modal size (e.g. "lg", "xl"). Empty renders the default width.
    size?: string;

    // When true, the body scrolls within the modal instead of growing the page.
    scrollable?: boolean;
}

export function Modal({
    title = '',
    visible,
    onClose,
    children,
    footerContent,
    size = '',
    scrollable = false,
}: ModalProps) {
    if (!visible) {
        return null;
    }

    const sizeClass = size.trim().length === 0 ? '' : `modal-${size}`;
    const scrollableClass = scrollable ? 'modal-dialog-scrollable' : '';

    return (
        <>
            <div className="modal fade show" style={{ display: 'block' }} tabIndex={-1} role="dialog">
                <div className={`modal-dialog modal-dialog-centered ${sizeClass} ${scrollableClass}`} role="document">
                    <div className="modal-content">
                        <div className="modal-header">
                            <div className="modal-title fs-5">{title}</div>
                            <button type="button" className="btn-close" aria-label="Close"
                                onClick={onClose}></button>
                        </div>
                        <div className="modal-body">
                            {children}
                        </div>
                        {footerContent != null && (
                            <div className="modal-footer">
                                {footerContent}
                            </div>
                        )}
                    </div>
                </div>
            </div>
            <div className="modal-backdrop fade show"></div>
        </>
    );
}
