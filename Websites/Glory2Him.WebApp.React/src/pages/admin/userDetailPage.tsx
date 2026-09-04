import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Avatar } from '../../components/coreUI/avatar';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Button } from '../../components/coreUI/button';
import { Card } from '../../components/coreUI/card';
import { ConfirmDialog } from '../../components/coreUI/confirmDialog';
import { FormDate } from '../../components/coreUI/formDate';
import { FormSelect } from '../../components/coreUI/formSelect';
import { FormText } from '../../components/coreUI/formText';
import { Spinner } from '../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';
import { SelectOption } from '../../models/coreUI/selectOption';
import { UpdateUserRequest } from '../../models/admin/userView';
import { userAdminService } from '../../services/foundations/userAdminService';
import { extractApiErrorMessage } from './apiErrorMessage';

// Ported from the Blazor Admin/UserDetailPage: every card on this page acts on the user and
// re-renders in place.

const usersRoute = '/Admin/Users';

// Design §18.3.1 — a username may never be an email address, because every display name in the
// system falls back to the username, so an account with no personal details set publishes it.
const prohibitedUsernameCharacter = '@';

const prohibitedUsernameMessage =
    'A username may not contain "@". Usernames and email addresses are separate values: the '
    + 'username is shown to other people wherever the site names who submitted or reviewed '
    + 'something, so an email address used as one becomes public.';

const emptyEditModel: UpdateUserRequest = {
    userName: '',
    email: '',
    phoneNumber: '',
    name: '',
    surname: '',
    preferredName: null,
    dateOfBirth: null,
};

// FormDate speaks Date; the stored date of birth is a plain "yyyy-MM-dd" calendar date.
function toDateOfBirthValue(dateOfBirth: string | null): Date | null {
    if (dateOfBirth == null || dateOfBirth.length === 0) {
        return null;
    }

    const parsed = Date.parse(`${dateOfBirth.slice(0, 10)}T00:00:00`);

    return Number.isNaN(parsed) ? null : new Date(parsed);
}

function toDateOfBirthString(value: Date | null): string | null {
    if (value == null) {
        return null;
    }

    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');

    return `${value.getFullYear()}-${month}-${day}`;
}

export const UserDetailPage = () => {
    const { userId = '' } = useParams();
    const navigate = useNavigate();

    const { data: user, isLoading: isUserLoading, isError: isUserError, refetch } =
        userAdminService.useGetUserById(userId);

    const { data: allRoleNames, isLoading: areRolesLoading, isError: isRolesError } =
        userAdminService.useGetAllRoles();

    const updateUser = userAdminService.useUpdateUser();
    const setUserRole = userAdminService.useSetUserRole();
    const confirmEmail = userAdminService.useConfirmEmail();
    const setLockedOut = userAdminService.useSetLockedOut();
    const resetFailedCount = userAdminService.useResetFailedCount();
    const setTwoFactor = userAdminService.useSetTwoFactor();
    const setDisabled = userAdminService.useSetDisabled();
    const getConfirmationLink = userAdminService.useGetConfirmationLink();
    const getPasswordResetLink = userAdminService.useGetPasswordResetLink();
    const deleteUser = userAdminService.useDeleteUser();

    const [editModel, setEditModel] = useState<UpdateUserRequest>(emptyEditModel);
    const [selectedRoleToAdd, setSelectedRoleToAdd] = useState<string | undefined>(undefined);
    const [actionError, setActionError] = useState<string | null>(null);
    const [actionMessage, setActionMessage] = useState<string | null>(null);
    const [generatedLink, setGeneratedLink] = useState<string | null>(null);
    const [generatedLinkLabel, setGeneratedLinkLabel] = useState<string | null>(null);
    const [isDeleteDialogVisible, setIsDeleteDialogVisible] = useState(false);

    const isLoading = isUserLoading || areRolesLoading;
    const hasError = isUserError || isRolesError;

    useEffect(() => {
        document.title = user == null ? 'User — Glory 2 Him' : `${user.userName} — Glory 2 Him`;
    }, [user]);

    // Edit a copy so an abandoned edit never leaves the displayed user half-changed.
    useEffect(() => {
        if (user == null) {
            return;
        }

        setEditModel({
            userName: user.userName,
            email: user.email,
            phoneNumber: user.phoneNumber,
            name: user.name,
            surname: user.surname,
            preferredName: user.preferredName,
            dateOfBirth: user.dateOfBirth,
        });
    }, [user]);

    const availableRoleOptions = useMemo<SelectOption[]>(() => {
        if (user == null || allRoleNames == null) {
            return [];
        }

        return allRoleNames
            .filter((role) => !user.roles.includes(role))
            .map((role) => ({ text: role, value: role }));
    }, [user, allRoleNames]);

    useEffect(() => {
        setSelectedRoleToAdd((current) =>
            current != null && availableRoleOptions.some((option) => option.value === current)
                ? current
                : availableRoleOptions[0]?.value);
    }, [availableRoleOptions]);

    const crumbs: BreadcrumbItem[] = [
        { title: 'Admin' },
        { title: 'Users', href: usersRoute },
        { title: user?.userName ?? 'User', isActive: true },
    ];

    const deleteMessage =
        user == null
            ? 'Are you sure?'
            : `Permanently delete "${user.userName}"? This cannot be undone. `
                + 'Disabling the account keeps its history and can be reversed.';

    const clearNotices = () => {
        setActionError(null);
        setActionMessage(null);
        setGeneratedLink(null);
        setGeneratedLinkLabel(null);
    };

    // Every action follows the same shape: clear the last notice, act, re-read the user so the
    // badges and roles reflect what just happened, then report the outcome.
    const runAsync = async (action: () => Promise<unknown>, successMessage: string) => {
        clearNotices();

        try {
            await action();
            await refetch();

            setActionMessage(successMessage);
        } catch (error) {
            setActionError(extractApiErrorMessage(
                error, 'The action could not be completed. Please try again.'));
        }
    };

    const generateLinkAsync = async (linkFactory: () => Promise<string>, label: string) => {
        clearNotices();

        try {
            const link = await linkFactory();

            setGeneratedLink(link);
            setGeneratedLinkLabel(label);
        } catch (error) {
            setActionError(extractApiErrorMessage(
                error, 'The link could not be generated. Please try again.'));
        }
    };

    // Mirrors UserNameRule on the server (design §18.3.1), which refuses this too. The point of
    // repeating it here is the message: without it the administrator gets Identity's own
    // "Username 'x@y.org' is invalid, can only contain letters or digits", which says nothing
    // about why the rule exists or that it is deliberate.
    const saveProfileAsync = () => {
        if (editModel.userName.includes(prohibitedUsernameCharacter)) {
            clearNotices();
            setActionError(prohibitedUsernameMessage);

            return Promise.resolve();
        }

        return runAsync(
            () => updateUser.mutateAsync({ userId, request: editModel }),
            'Profile updated.');
    };

    const addRoleAsync = () => {
        if (selectedRoleToAdd == null || selectedRoleToAdd.trim().length === 0) {
            return;
        }

        const roleName = selectedRoleToAdd;

        void runAsync(
            () => setUserRole.mutateAsync({ userId, roleName, isInRole: true }),
            `Added to ${roleName}.`);
    };

    const removeRoleAsync = (roleName: string) =>
        runAsync(
            () => setUserRole.mutateAsync({ userId, roleName, isInRole: false }),
            `Removed from ${roleName}.`);

    const confirmEmailAsync = () =>
        runAsync(() => confirmEmail.mutateAsync(userId), 'Email confirmed.');

    const setLockedOutAsync = (isLockedOut: boolean) =>
        runAsync(
            () => setLockedOut.mutateAsync({ userId, isLockedOut }),
            isLockedOut ? 'User locked out.' : 'User unlocked.');

    const resetFailedCountAsync = () =>
        runAsync(() => resetFailedCount.mutateAsync(userId), 'Failed login count reset.');

    const setTwoFactorAsync = (isEnabled: boolean) =>
        runAsync(
            () => setTwoFactor.mutateAsync({ userId, isEnabled }),
            isEnabled ? 'Two-factor enabled.' : 'Two-factor disabled.');

    const setDisabledAsync = (isDisabled: boolean) =>
        runAsync(
            () => setDisabled.mutateAsync({ userId, isDisabled }),
            isDisabled ? 'Account disabled.' : 'Account enabled.');

    const generateConfirmationLinkAsync = () =>
        generateLinkAsync(
            () => getConfirmationLink.mutateAsync(userId),
            'Email confirmation link — share this with the user:');

    const generateResetLinkAsync = () =>
        generateLinkAsync(
            () => getPasswordResetLink.mutateAsync(userId),
            'Password reset link — share this with the user:');

    const openDeleteDialog = () => {
        clearNotices();
        setIsDeleteDialogVisible(true);
    };

    const closeDeleteDialog = () => setIsDeleteDialogVisible(false);

    const confirmDeleteAsync = async () => {
        setIsDeleteDialogVisible(false);
        clearNotices();

        try {
            await deleteUser.mutateAsync(userId);

            navigate(usersRoute);
        } catch (error) {
            setActionError(extractApiErrorMessage(
                error, 'The user could not be deleted. Please try again.'));
        }
    };

    const goBack = () => navigate(usersRoute);

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">User</h1>
                <Breadcrumb items={crumbs} />
            </div>
            <hr />

            {isLoading ? (
                <div className="text-center py-5">
                    <Spinner />
                </div>
            ) : hasError ? (
                <>
                    <div className="alert alert-danger" role="alert">
                        We could not load this user right now. Please try again later.
                    </div>
                    <Button color="secondary" onClick={goBack}>Back to Users</Button>
                </>
            ) : user != null && (
                <>
                    <div className="d-flex justify-content-end mb-3">
                        <Button color="secondary" onClick={goBack}>
                            <i className="bi bi-arrow-left me-1"></i>Back to Users
                        </Button>
                    </div>

                    {actionError != null && (
                        <div className="alert alert-danger" role="alert">{actionError}</div>
                    )}
                    {actionMessage != null && (
                        <div className="alert alert-success" role="alert">{actionMessage}</div>
                    )}
                    {generatedLink != null && (
                        <div className="alert alert-info" role="alert">
                            <div className="fw-semibold mb-2">{generatedLinkLabel}</div>
                            <textarea
                                className="form-control font-monospace small"
                                rows={3}
                                readOnly
                                value={generatedLink}></textarea>
                        </div>
                    )}

                    <Card cssClass="mb-4" headerContent="User details">
                        <div className="d-flex align-items-center gap-3 mb-4">
                            <Avatar name={user.displayName} imageUrl={user.imageUrl ?? undefined} sizePx={64} />
                            <div>
                                <div className="h5 mb-1">{user.displayName}</div>
                                <div className="text-body-secondary small font-monospace">{user.id}</div>
                            </div>
                        </div>

                        <div className="d-flex flex-wrap gap-2 mb-4">
                            <span className={`badge ${user.emailConfirmed ? 'text-bg-success' : 'text-bg-secondary'}`}>
                                Email {user.emailConfirmed ? 'confirmed' : 'unconfirmed'}
                            </span>
                            <span className={`badge ${user.isLockedOut ? 'text-bg-danger' : 'text-bg-success'}`}>
                                {user.isLockedOut ? 'Locked out' : 'Not locked'}
                            </span>
                            <span className={`badge ${user.twoFactorEnabled ? 'text-bg-info' : 'text-bg-secondary'}`}>
                                2FA {user.twoFactorEnabled ? 'on' : 'off'}
                            </span>
                            <span className={`badge ${user.isDisabled ? 'text-bg-danger' : 'text-bg-success'}`}>
                                {user.isDisabled ? 'Disabled' : 'Active'}
                            </span>
                            <span className="badge text-bg-light text-dark border">
                                Failed logins: {user.accessFailedCount}
                            </span>
                        </div>

                        <div className="row">
                            <div className="col-md-6">
                                <FormText label="Username" value={editModel.userName}
                                    onValueChange={(value) => setEditModel({ ...editModel, userName: value })} />
                                <small className="form-text d-block mt-n3 mb-3">
                                    Shown to other people. It may not be an email address.
                                </small>
                            </div>
                            <div className="col-md-6">
                                <FormText label="Email" value={editModel.email}
                                    onValueChange={(value) => setEditModel({ ...editModel, email: value })} />
                            </div>
                        </div>

                        <div className="row">
                            <div className="col-md-6">
                                <FormText label="First name" value={editModel.name}
                                    onValueChange={(value) => setEditModel({ ...editModel, name: value })} />
                            </div>
                            <div className="col-md-6">
                                <FormText label="Surname" value={editModel.surname}
                                    onValueChange={(value) => setEditModel({ ...editModel, surname: value })} />
                            </div>
                        </div>

                        <div className="row">
                            <div className="col-md-6">
                                <FormText label="Preferred name" value={editModel.preferredName ?? ''}
                                    onValueChange={(value) =>
                                        setEditModel({ ...editModel, preferredName: value.length === 0 ? null : value })} />
                            </div>
                            <div className="col-md-6">
                                <FormText label="Phone number" value={editModel.phoneNumber ?? ''}
                                    onValueChange={(value) => setEditModel({ ...editModel, phoneNumber: value })} />
                            </div>
                        </div>

                        <div className="row">
                            <div className="col-md-6">
                                <FormDate label="Date of birth" value={toDateOfBirthValue(editModel.dateOfBirth)}
                                    onValueChange={(value) =>
                                        setEditModel({ ...editModel, dateOfBirth: toDateOfBirthString(value) })} />
                            </div>
                        </div>

                        <Button color="primary" onClick={() => void saveProfileAsync()}>Save profile</Button>
                    </Card>

                    <Card cssClass="mb-4" headerContent="Roles">
                        {user.roles.length > 0 ? (
                            <ul className="list-group mb-3">
                                {user.roles.map((role) => (
                                    <li key={role} className="list-group-item d-flex justify-content-between align-items-center">
                                        <span className="badge text-bg-primary">{role}</span>
                                        <Button color="outline-danger" cssClass="btn-sm"
                                            onClick={() => void removeRoleAsync(role)}>
                                            Remove
                                        </Button>
                                    </li>
                                ))}
                            </ul>
                        ) : (
                            <p className="text-body-secondary">This user has no roles assigned.</p>
                        )}

                        {availableRoleOptions.length > 0 ? (
                            <div className="row g-2 align-items-end">
                                <div className="col-sm-6">
                                    <FormSelect label="Add role" options={availableRoleOptions}
                                        value={selectedRoleToAdd ?? ''}
                                        onValueChange={(value) => setSelectedRoleToAdd(value)} />
                                </div>
                                <div className="col-sm-auto mb-3">
                                    <Button color="primary" onClick={addRoleAsync}>Add</Button>
                                </div>
                            </div>
                        ) : (
                            <p className="text-body-secondary mb-0">This user already holds every role.</p>
                        )}
                    </Card>

                    <Card cssClass="mb-4" headerContent="Account actions">
                        <div className="d-flex flex-wrap gap-2">
                            {!user.emailConfirmed && (
                                <Button color="success" cssClass="btn-sm" onClick={() => void confirmEmailAsync()}>
                                    Confirm email
                                </Button>
                            )}
                            <Button color="outline-secondary" cssClass="btn-sm"
                                onClick={() => void generateConfirmationLinkAsync()}>
                                Email confirmation link
                            </Button>
                            <Button color="outline-secondary" cssClass="btn-sm"
                                onClick={() => void generateResetLinkAsync()}>
                                Password reset link
                            </Button>

                            {user.isLockedOut ? (
                                <Button color="warning" cssClass="btn-sm"
                                    onClick={() => void setLockedOutAsync(false)}>
                                    Unlock
                                </Button>
                            ) : (
                                <Button color="warning" cssClass="btn-sm"
                                    onClick={() => void setLockedOutAsync(true)}>
                                    Lock
                                </Button>
                            )}

                            <Button color="outline-secondary" cssClass="btn-sm"
                                onClick={() => void resetFailedCountAsync()}>
                                Reset failed count
                            </Button>

                            {user.twoFactorEnabled ? (
                                <Button color="outline-secondary" cssClass="btn-sm"
                                    onClick={() => void setTwoFactorAsync(false)}>
                                    Disable 2FA
                                </Button>
                            ) : (
                                <Button color="outline-secondary" cssClass="btn-sm"
                                    onClick={() => void setTwoFactorAsync(true)}>
                                    Enable 2FA
                                </Button>
                            )}

                            {user.isDisabled ? (
                                <Button color="success" cssClass="btn-sm"
                                    onClick={() => void setDisabledAsync(false)}>
                                    Enable user
                                </Button>
                            ) : (
                                <Button color="danger" cssClass="btn-sm"
                                    onClick={() => void setDisabledAsync(true)}>
                                    Disable user
                                </Button>
                            )}

                            <Button color="danger" cssClass="btn-sm" onClick={openDeleteDialog}>
                                Delete user
                            </Button>
                        </div>
                    </Card>

                    <ConfirmDialog
                        visible={isDeleteDialogVisible}
                        title="Delete user"
                        message={deleteMessage}
                        confirmText="Delete"
                        onConfirm={() => void confirmDeleteAsync()}
                        onCancel={closeDeleteDialog} />
                </>
            )}
        </>
    );
};
