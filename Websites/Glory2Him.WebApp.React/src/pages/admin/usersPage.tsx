import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Avatar } from '../../components/coreUI/avatar';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Button } from '../../components/coreUI/button';
import { Card } from '../../components/coreUI/card';
import { DataTable } from '../../components/coreUI/dataTable';
import { Spinner } from '../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';
import { DataTableColumn } from '../../models/coreUI/dataTableColumn';
import { UserView } from '../../models/admin/userView';
import { userAdminService } from '../../services/foundations/userAdminService';

// Ported from the Blazor Admin/UsersPage: the DataTable sorts, pages and searches client-side.

const crumbs: BreadcrumbItem[] = [
    { title: 'Admin' },
    { title: 'Users', href: '/Admin/Users', isActive: true },
];

const columns: DataTableColumn<UserView>[] = [
    {
        title: '',
        sortable: false,
        value: (user) => user.userName,
        cellTemplate: (user) =>
            <Avatar name={user.displayName} imageUrl={user.imageUrl ?? undefined} sizePx={36} />,
    },
    {
        title: 'Username',
        value: (user) => user.userName,
    },
    {
        title: 'Email',
        value: (user) => user.email,
    },
    {
        title: 'Roles',
        value: (user) => user.roles.join(', '),
        cellTemplate: (user) =>
            <span>
                {user.roles.map((role) => (
                    <span key={role} className="badge text-bg-primary me-1">{role}</span>
                ))}
            </span>,
    },
    {
        title: 'Status',
        value: (user) => (user.isDisabled ? 'Disabled' : 'Active'),
        cellTemplate: (user) =>
            <span className={`badge ${user.isDisabled ? 'text-bg-danger' : 'text-bg-success'}`}>
                {user.isDisabled ? 'Disabled' : 'Active'}
            </span>,
    },
];

export const UsersPage = () => {
    const navigate = useNavigate();
    const { data: users, isLoading, isError } = userAdminService.useGetAllUsers();

    useEffect(() => {
        document.title = 'Users — Glory 2 Him';
    }, []);

    // Everything a user can be changed to lives on its own addressable page, so the list
    // only routes there.
    const viewUser = (userId: string) => navigate(`/Admin/Users/${userId}`);

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">Users</h1>
                <Breadcrumb items={crumbs} />
            </div>
            <hr />

            {isLoading ? (
                <div className="text-center py-5">
                    <Spinner />
                </div>
            ) : isError ? (
                <div className="alert alert-danger" role="alert">
                    We could not load users right now. Please try again later.
                </div>
            ) : (users == null || users.length === 0) ? (
                <div className="alert alert-info" role="alert">No users found.</div>
            ) : (
                <Card>
                    <DataTable
                        items={users}
                        columns={columns}
                        pageSize={10}
                        rowActions={(user) => (
                            <Button color="outline-primary" cssClass="btn-sm" onClick={() => viewUser(user.id)}>
                                View
                            </Button>
                        )} />
                </Card>
            )}
        </>
    );
};
