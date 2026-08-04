import { Link } from 'react-router-dom';
import ManageAccountBroker from '../../../brokers/apiBroker.manageAccounts';

// Ported from Blazor's Account/Pages/Manage/PersonalData.razor. The Blazor page posted a
// form to the DownloadPersonalData endpoint; here the button navigates to the cookie-
// authenticated download endpoint, which answers with the same Content-Disposition
// attachment, so the browser saves PersonalData.json exactly as before.
export function PersonalData() {
    const manageAccountBroker = new ManageAccountBroker();

    const downloadPersonalData = () => {
        window.location.href = manageAccountBroker.personalDataDownloadUrl;
    };

    return (
        <>
            <h3>Personal Data</h3>

            <div className="row">
                <div className="col-md-6">
                    <p>Your account contains personal data that you have given us. This page allows you to download or delete that data.</p>
                    <p>
                        <strong>Deleting this data will permanently remove your account, and this cannot be recovered.</strong>
                    </p>
                    <form
                        onSubmit={(event) => {
                            event.preventDefault();
                            downloadPersonalData();
                        }}
                        method="post">
                        <button className="btn btn-primary" type="submit">Download</button>
                    </form>
                    <p>
                        <Link to="/Account/Manage/DeletePersonalData" className="btn btn-danger">Delete</Link>
                    </p>
                </div>
            </div>
        </>
    );
}
