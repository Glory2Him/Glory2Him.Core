import { ReactElement } from 'react';
import { NavLink } from 'react-router-dom';

// Ported from Blazor's Account/Shared/ManageNavMenu.razor, trimmed to the flows the React
// app supports: Profile and Password. Email, external logins, two-factor authentication,
// passkeys, participant management and personal data have no API endpoints yet.
export default function ManageNavMenu(): ReactElement {
    return (
        <ul className="nav nav-pills flex-column">
            <li className="nav-item">
                <NavLink className="nav-link" to="/Account/Manage" end>Profile</NavLink>
            </li>
            <li className="nav-item">
                <NavLink className="nav-link" to="/Account/Manage/ChangePassword">Password</NavLink>
            </li>
        </ul>
    );
}
