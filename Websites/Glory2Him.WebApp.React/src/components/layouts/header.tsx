import { Link } from "react-router-dom";
import { NavDropdown } from "react-bootstrap";
import { useAuth } from "../securitys/authProvider";
import { UserProfile } from "../securitys/userProfile";
import { SecuredLink } from "../securitys/securedLinks";
import { accountService } from "../../services/foundations/accountService";
import securityPoints from "../../securityMatrix";

// Placeholder header carrying the Blogzine navbar structure. The full
// HeaderComponent conversion (mega menu, offcanvas, search) follows in the
// page-conversion work.
export default function HeaderComponent() {
    const { isAuthenticated, user } = useAuth();
    const logout = accountService.useLogout();

    return (
        <header className="navbar-light navbar-sticky header-static">
            <nav className="navbar navbar-expand-lg">
                <div className="container">
                    <Link className="navbar-brand" to="/">
                        <span className="h4 mb-0">Glory 2 Him</span>
                    </Link>
                    <div className="navbar-collapse collapse show">
                        <ul className="navbar-nav navbar-nav-scroll ms-auto align-items-center">
                            <li className="nav-item">
                                <Link className="nav-link" to="/">Home</Link>
                            </li>
                            <li className="nav-item">
                                <SecuredLink to="/Admin/Dashboard" className="nav-link" allowedRoles={securityPoints.admin.view}>
                                    Admin
                                </SecuredLink>
                            </li>
                            <li className="nav-item">
                                {isAuthenticated ? (
                                    <NavDropdown title={user?.displayName || user?.userName} align="end">
                                        <UserProfile />
                                        <NavDropdown.Item onClick={() => logout.mutate()}>Sign out</NavDropdown.Item>
                                    </NavDropdown>
                                ) : (
                                    <Link className="nav-link" to="/Account/Login">Sign in</Link>
                                )}
                            </li>
                        </ul>
                    </div>
                </div>
            </nav>
        </header>
    );
}
