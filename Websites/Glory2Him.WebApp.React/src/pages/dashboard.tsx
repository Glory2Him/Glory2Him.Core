import { useAuth } from "../components/securitys/authProvider";

export const Dashboard = () => {
    const { user } = useAuth();

    return (
        <div className="container mt-4">
            <div className="row">
                <div className="col-12 py-5">
                    <h1>Dashboard</h1>
                    <p className="lead">Welcome, {user?.displayName || user?.userName}.</p>
                </div>
            </div>
        </div>
    );
}
