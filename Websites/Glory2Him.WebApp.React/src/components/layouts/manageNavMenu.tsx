import { ReactElement } from 'react';
import { NavLink } from 'react-router-dom';

// Ported from Blazor's Account/Shared/ManageNavMenu.razor: Profile, Email, Password,
// Two-factor authentication, Passkeys, External logins and Personal data.
export default function ManageNavMenu(): ReactElement {
    return (
        <ul className="nav nav-pills flex-column">
            <li className="nav-item">
                <NavLink className="nav-link" to="/Account/Manage" end>Profile</NavLink>
            </li>
            <li className="nav-item">
                <NavLink className="nav-link" to="/Account/Manage/Email">Email</NavLink>
            </li>
            <li className="nav-item">
                <NavLink className="nav-link" to="/Account/Manage/ChangePassword">Password</NavLink>
            </li>
            <li className="nav-item">
                <NavLink className="nav-link" to="/Account/Manage/TwoFactorAuthentication">Two-factor authentication</NavLink>
            </li>
            <li className="nav-item">
                <NavLink className="nav-link" to="/Account/Manage/Passkeys">Passkeys</NavLink>
            </li>
            <li className="nav-item">
                <NavLink className="nav-link" to="/Account/Manage/ExternalLogins">External logins</NavLink>
            </li>
            <li className="nav-item">
                <NavLink className="nav-link" to="/Account/Manage/PersonalData">Personal data</NavLink>
            </li>
        </ul>
    );
}
