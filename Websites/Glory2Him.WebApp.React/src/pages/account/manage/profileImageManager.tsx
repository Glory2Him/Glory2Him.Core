import { ChangeEvent, useRef, useState } from 'react';
import { Avatar } from '../../../components/coreUI/avatar';
import { Spinner } from '../../../components/coreUI/spinner';
import { ProfileView } from '../../../models/profile/profileView';
import { profileService } from '../../../services/foundations/profileService';
import { extractApiErrorMessage } from '../statusMessage';

// Ported from Blazor's Account/Shared/ProfileImageManager.razor: shows the current avatar
// and lets the user upload a new profile image or remove it. Validation and resizing happen
// server-side; the react-query invalidation reloads the profile (and its imageUrl) after
// either operation, mirroring the Blazor component's ReloadAsync.
const maxUploadBytes = 5 * 1024 * 1024;

export interface ProfileImageManagerProps {
    profile: ProfileView;
    name: string;
}

export function ProfileImageManager({ profile, name }: ProfileImageManagerProps) {
    const [statusMessage, setStatusMessage] = useState<string | null>(null);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);
    const fileInput = useRef<HTMLInputElement>(null);

    const uploadImage = profileService.useUploadProfileImage();
    const deleteImage = profileService.useDeleteProfileImage();

    const isBusy = uploadImage.isPending || deleteImage.isPending;
    const hasImage = profile.hasProfileImage;
    const imageUrl = profile.imageUrl ?? undefined;

    const onFileSelected = async (event: ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        event.target.value = '';

        if (file == null) {
            return;
        }

        setStatusMessage(null);
        setErrorMessage(null);

        if (file.size > maxUploadBytes) {
            setErrorMessage('The image is too large. Please choose a file up to 5 MB.');
            return;
        }

        try {
            await uploadImage.mutateAsync(file);
            setStatusMessage('Your profile image has been updated.');
        } catch (error) {
            setErrorMessage(extractApiErrorMessage(
                error, 'We could not update your image. Please try again.'));
        }
    };

    const remove = async () => {
        setStatusMessage(null);
        setErrorMessage(null);

        try {
            await deleteImage.mutateAsync();
            setStatusMessage('Your profile image has been removed.');
        } catch {
            setErrorMessage('We could not remove your image. Please try again.');
        }
    };

    return (
        <div className="d-flex align-items-center flex-wrap gap-3">
            <Avatar name={name} imageUrl={imageUrl} sizePx={96} />

            <div>
                {isBusy && (
                    <div className="d-flex align-items-center mb-2">
                        <Spinner />
                        <span className="ms-2">Processing…</span>
                    </div>
                )}

                <label className={`btn btn-primary btn-sm mb-0 ${isBusy ? 'disabled' : ''}`}>
                    <i className="bi bi-upload me-1"></i>Upload image
                    <input
                        ref={fileInput}
                        type="file"
                        onChange={onFileSelected}
                        accept="image/png,image/jpeg,image/webp,image/gif"
                        hidden
                        disabled={isBusy} />
                </label>

                {hasImage && (
                    <button
                        type="button"
                        className="btn btn-outline-danger btn-sm mb-0 ms-2"
                        onClick={remove}
                        disabled={isBusy}>
                        <i className="bi bi-trash me-1"></i>Remove
                    </button>
                )}

                <div className="form-text mt-2">
                    PNG, JPEG, or WebP up to 5 MB. Images are cropped square and resized to 256×256.
                </div>

                {statusMessage != null && (
                    <div className="alert alert-success py-2 mt-2 mb-0" role="alert">{statusMessage}</div>
                )}
                {errorMessage != null && (
                    <div className="alert alert-danger py-2 mt-2 mb-0" role="alert">{errorMessage}</div>
                )}
            </div>
        </div>
    );
}
