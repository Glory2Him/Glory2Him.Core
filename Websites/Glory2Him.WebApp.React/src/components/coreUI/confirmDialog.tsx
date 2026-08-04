import { Button } from './button';
import { Modal } from './modal';

export interface ConfirmDialogProps {
    visible: boolean;
    title?: string;
    message?: string;
    confirmText?: string;
    cancelText?: string;
    confirmColor?: string;
    onConfirm?: () => void;
    onCancel?: () => void;
}

export function ConfirmDialog({
    visible,
    title = 'Are you sure?',
    message,
    confirmText = 'OK',
    cancelText = 'Cancel',
    confirmColor = 'danger',
    onConfirm,
    onCancel,
}: ConfirmDialogProps) {
    return (
        <Modal
            title={title}
            visible={visible}
            onClose={onCancel}
            footerContent={
                <>
                    <Button color="secondary" onClick={onCancel}>{cancelText}</Button>
                    <Button color={confirmColor} onClick={onConfirm}>{confirmText}</Button>
                </>
            }>
            <p className="mb-0">{message}</p>
        </Modal>
    );
}
