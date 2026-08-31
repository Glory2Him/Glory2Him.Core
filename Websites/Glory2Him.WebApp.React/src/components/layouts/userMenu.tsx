import { ReactElement, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../securitys/authProvider";
import { SecuredComponent } from "../securitys/securedComponents";
import { accountService } from "../../services/foundations/accountService";
import securityPoints from "../../securityMatrix";
import { Avatar } from "../coreUI/avatar";

// CoreUI-style notification bell + profile avatar dropdown, ported from the Blazor
// UserMenuComponent: restyled to the Blogzine theme and identity-aware via useAuth. Bootstrap
// dropdowns (data-bs-toggle) work through the globally loaded bootstrap.bundle.min.js, whose
// data API delegates clicks at the document level and therefore reaches React-rendered DOM.
const notificationCount = 3;

export default function UserMenuComponent(): ReactElement {
    const { isAuthenticated, user } = useAuth();
    const logout = accountService.useLogout();

    // The signed-in user's avatar URL (undefined → the Avatar shows initials). The profile
    // image endpoint is probed up front so a user without an uploaded image degrades
    // gracefully to the initials circle instead of a broken <img>.
    const [avatarImageUrl, setAvatarImageUrl] = useState<string | undefined>(undefined);

    const userId = user?.userId;

    useEffect(() => {
        setAvatarImageUrl(undefined);

        if (!userId) {
            return;
        }

        const profileImageUrl = `/Profile-Image/${userId}`;
        const probe = new Image();
        let cancelled = false;

        probe.onload = () => {
            if (!cancelled) {
                setAvatarImageUrl(profileImageUrl);
            }
        };

        probe.src = profileImageUrl;

        return () => {
            cancelled = true;
        };
    }, [userId]);

    if (!isAuthenticated) {
        return (
            // Anonymous: a simple sign-in icon in the same spot
            <div className="nav-item ms-2">
                <Link className="nav-link p-0" to="/Account/Login" aria-label="Sign in">
                    <i className="bi bi-person-circle fs-4"></i>
                </Link>
            </div>
        );
    }

    return (
        <>
            {/* Notification bell */}
            <div className="nav-item dropdown ms-2">
                <a className="nav-link position-relative p-0" role="button" href="#" id="notificationMenu"
                    data-bs-toggle="dropdown" data-bs-auto-close="outside" aria-expanded="false"
                    aria-label="Notifications">
                    <i className="bi bi-bell fs-4"></i>
                    <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger p-1">
                        <span className="visually-hidden">unread notifications</span>
                    </span>
                </a>
                <div className="dropdown-menu dropdown-menu-end shadow rounded p-0" aria-labelledby="notificationMenu" style={{ minWidth: "20rem" }}>
                    <div className="d-flex justify-content-between align-items-center bg-light px-3 py-2 rounded-top">
                        <span className="fw-bold">Notifications</span>
                        <span className="badge text-bg-danger">{notificationCount} new</span>
                    </div>
                    <ul className="list-unstyled mb-0">
                        <li>
                            <a className="dropdown-item d-flex align-items-start py-2 border-bottom" href="#">
                                <i className="bi bi-heart-fill text-danger me-2 mt-1"></i>
                                <span>
                                    <span className="d-block">Someone liked your post</span>
                                    <small className="text-body-secondary">2 hours ago</small>
                                </span>
                            </a>
                        </li>
                        <li>
                            <a className="dropdown-item d-flex align-items-start py-2 border-bottom" href="#">
                                <i className="bi bi-chat-left-text-fill text-primary me-2 mt-1"></i>
                                <span>
                                    <span className="d-block">New comment on your article</span>
                                    <small className="text-body-secondary">Yesterday</small>
                                </span>
                            </a>
                        </li>
                        <li>
                            <a className="dropdown-item d-flex align-items-start py-2" href="#">
                                <i className="bi bi-person-plus-fill text-success me-2 mt-1"></i>
                                <span>
                                    <span className="d-block">A new subscriber joined</span>
                                    <small className="text-body-secondary">3 days ago</small>
                                </span>
                            </a>
                        </li>
                    </ul>
                    <a className="dropdown-item text-center text-primary border-top py-2 rounded-bottom" href="#">
                        View all notifications
                    </a>
                </div>
            </div>

            {/* Profile avatar + account menu */}
            <div className="nav-item dropdown ms-3">
                <a className="nav-link p-0" role="button" href="#" id="profileMenu"
                    data-bs-toggle="dropdown" aria-expanded="false" aria-label="Account menu">
                    <Avatar name={user?.userName ?? ""} imageUrl={avatarImageUrl} sizePx={36} />
                </a>
                <ul className="dropdown-menu dropdown-menu-end shadow rounded" aria-labelledby="profileMenu" style={{ minWidth: "15rem" }}>
                    <li className="px-3 py-2 bg-light rounded-top">
                        <span className="d-block text-uppercase small text-body-secondary">Account</span>
                        <span className="fw-bold">{user?.userName}</span>
                    </li>
                    <li><Link className="dropdown-item d-flex align-items-center" to="/Dashboard"><i className="bi bi-bell fa-fw me-2"></i>Updates <span className="badge text-bg-primary ms-auto">8</span></Link></li>
                    <li><a className="dropdown-item d-flex align-items-center" href="#"><i className="bi bi-envelope fa-fw me-2"></i>Messages <span className="badge text-bg-success ms-auto">42</span></a></li>
                    <li><a className="dropdown-item d-flex align-items-center" href="#"><i className="bi bi-check2-square fa-fw me-2"></i>Tasks</a></li>
                    <li><a className="dropdown-item d-flex align-items-center" href="#"><i className="bi bi-chat-left fa-fw me-2"></i>Comments</a></li>
                    <li><Link className="dropdown-item d-flex align-items-center" to="/MyPosts"><i className="bi bi-journal-text fa-fw me-2"></i>My Posts</Link></li>

                    <li><hr className="dropdown-divider" /></li>
                    <li className="px-3 pt-1"><span className="d-block text-uppercase small text-body-secondary">Settings</span></li>
                    <li><Link className="dropdown-item d-flex align-items-center" to="/Account/Manage"><i className="bi bi-person fa-fw me-2"></i>Profile</Link></li>
                    <li><Link className="dropdown-item d-flex align-items-center" to="/Account/Manage/Email"><i className="bi bi-gear fa-fw me-2"></i>Settings</Link></li>
                    <li><Link className="dropdown-item d-flex align-items-center" to="/Account/Manage/ChangePassword"><i className="bi bi-shield-lock fa-fw me-2"></i>Password</Link></li>
                    <SecuredComponent allowedRoles={securityPoints.admin.view}>
                        <li><Link className="dropdown-item d-flex align-items-center" to="/Admin/SamplePosts"><i className="bi bi-files fa-fw me-2"></i>Projects</Link></li>
                    </SecuredComponent>

                    <li><hr className="dropdown-divider" /></li>
                    <li>
                        <button type="button" onClick={() => logout.mutate()}
                            className="dropdown-item d-flex align-items-center text-danger">
                            <i className="bi bi-box-arrow-right fa-fw me-2"></i>Logout
                        </button>
                    </li>
                </ul>
            </div>
        </>
    );
}
