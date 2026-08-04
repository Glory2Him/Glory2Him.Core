import { FormEvent, useEffect, useState } from 'react';
import { Spinner } from '../../../components/coreUI/spinner';
import { profileService } from '../../../services/foundations/profileService';
import { StatusMessage, extractApiErrorMessage } from '../statusMessage';
import { ProfileImageManager } from './profileImageManager';

// Ported from Blazor's Account/Pages/Manage/Index.razor: personal details form plus the
// profile image manager. Data comes from the profile API instead of UserManager.
interface ValidationMessages {
    name?: string;
    surname?: string;
    preferredName?: string;
}

export function ManageIndex() {
    const { data: profile, isLoading } = profileService.useGetMyProfile();
    const updateProfile = profileService.useUpdateMyProfile();

    const [name, setName] = useState('');
    const [surname, setSurname] = useState('');
    const [preferredName, setPreferredName] = useState('');
    const [dateOfBirth, setDateOfBirth] = useState('');
    const [phoneNumber, setPhoneNumber] = useState('');
    const [validationMessages, setValidationMessages] = useState<ValidationMessages>({});
    const [message, setMessage] = useState<string | null>(null);

    useEffect(() => {
        if (profile != null) {
            setName(profile.name);
            setSurname(profile.surname);
            setPreferredName(profile.preferredName ?? '');
            setDateOfBirth(profile.dateOfBirth?.substring(0, 10) ?? '');
            setPhoneNumber(profile.phoneNumber ?? '');
        }
    }, [profile]);

    const validate = (): boolean => {
        const messages: ValidationMessages = {};

        if (name.trim().length === 0) {
            messages.name = 'The First name field is required.';
        } else if (name.length > 100) {
            messages.name =
                'The field First name must be a string with a minimum length of 1 and a maximum length of 100.';
        }

        if (surname.trim().length === 0) {
            messages.surname = 'The Surname field is required.';
        } else if (surname.length > 100) {
            messages.surname =
                'The field Surname must be a string with a minimum length of 1 and a maximum length of 100.';
        }

        if (preferredName.length > 100) {
            messages.preferredName =
                'The field Preferred name must be a string with a maximum length of 100.';
        }

        setValidationMessages(messages);

        return Object.keys(messages).length === 0;
    };

    const onValidSubmit = (event: FormEvent) => {
        event.preventDefault();
        setMessage(null);

        if (!validate()) {
            return;
        }

        updateProfile.mutate({
            name,
            surname,
            preferredName: preferredName.trim().length === 0 ? null : preferredName,
            dateOfBirth: dateOfBirth.length === 0 ? null : dateOfBirth,
            phoneNumber: phoneNumber.trim().length === 0 ? null : phoneNumber,
        }, {
            onSuccess: () => setMessage('Your profile has been updated'),
            onError: (error: unknown) => {
                setMessage(extractApiErrorMessage(error, 'Error: Failed to update your profile.'));
            }
        });
    };

    const summaryMessages = Object.values(validationMessages)
        .filter((validationMessage): validationMessage is string => validationMessage != null);

    if (isLoading || profile == null) {
        return <Spinner />;
    }

    return (
        <>
            <h3>Profile</h3>
            <StatusMessage message={message} />

            <div className="card card-body border mb-4">
                <h5 className="mb-3">Profile image</h5>
                <ProfileImageManager profile={profile} name={profile.userName} />
            </div>

            <div className="row">
                <div className="col-xl-6">
                    <form onSubmit={onValidSubmit} noValidate>
                        {summaryMessages.length > 0 && (
                            <ul className="text-danger" role="alert">
                                {summaryMessages.map((summaryMessage) =>
                                    <li key={summaryMessage}>{summaryMessage}</li>)}
                            </ul>
                        )}
                        <div className="form-floating mb-3">
                            <input
                                type="text"
                                value={profile.userName}
                                id="username"
                                className="form-control"
                                placeholder="Choose your username."
                                disabled />
                            <label htmlFor="username" className="form-label">Username</label>
                        </div>
                        <div className="row">
                            <div className="col-md-6">
                                <div className="form-floating mb-3">
                                    <input
                                        value={name}
                                        onChange={(event) => setName(event.target.value)}
                                        id="Input.Name"
                                        className="form-control"
                                        placeholder="First name" />
                                    <label htmlFor="Input.Name" className="form-label">First name</label>
                                    {validationMessages.name != null && (
                                        <div className="text-danger">{validationMessages.name}</div>
                                    )}
                                </div>
                            </div>
                            <div className="col-md-6">
                                <div className="form-floating mb-3">
                                    <input
                                        value={surname}
                                        onChange={(event) => setSurname(event.target.value)}
                                        id="Input.Surname"
                                        className="form-control"
                                        placeholder="Surname" />
                                    <label htmlFor="Input.Surname" className="form-label">Surname</label>
                                    {validationMessages.surname != null && (
                                        <div className="text-danger">{validationMessages.surname}</div>
                                    )}
                                </div>
                            </div>
                        </div>
                        <div className="row">
                            <div className="col-md-6">
                                <div className="form-floating mb-3">
                                    <input
                                        value={preferredName}
                                        onChange={(event) => setPreferredName(event.target.value)}
                                        id="Input.PreferredName"
                                        className="form-control"
                                        placeholder="Preferred name" />
                                    <label htmlFor="Input.PreferredName" className="form-label">Preferred name (optional)</label>
                                    {validationMessages.preferredName != null && (
                                        <div className="text-danger">{validationMessages.preferredName}</div>
                                    )}
                                </div>
                            </div>
                            <div className="col-md-6">
                                <div className="form-floating mb-3">
                                    <input
                                        type="date"
                                        value={dateOfBirth}
                                        onChange={(event) => setDateOfBirth(event.target.value)}
                                        id="Input.DateOfBirth"
                                        className="form-control"
                                        placeholder="Date of birth" />
                                    <label htmlFor="Input.DateOfBirth" className="form-label">Date of birth (optional)</label>
                                </div>
                            </div>
                        </div>
                        <div className="form-floating mb-3">
                            <input
                                value={phoneNumber}
                                onChange={(event) => setPhoneNumber(event.target.value)}
                                id="Input.PhoneNumber"
                                className="form-control"
                                placeholder="Enter your phone number" />
                            <label htmlFor="Input.PhoneNumber" className="form-label">Phone number</label>
                        </div>
                        <button
                            type="submit"
                            className="w-100 btn btn-lg btn-primary"
                            disabled={updateProfile.isPending}>
                            Save
                        </button>
                    </form>
                </div>
            </div>
        </>
    );
}
