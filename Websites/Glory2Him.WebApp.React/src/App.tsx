import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import "react-toastify/dist/ReactToastify.css";
import { queryClientGlobalOptions } from './brokers/apiBroker.globals';
import ToastBroker from './brokers/toastBroker';
import { AuthProvider } from './components/securitys/authProvider';
import { SecuredRoute } from './components/securitys/securedRoutes';
import securityPoints from './securityMatrix';
import Root from './components/root';
import ErrorPage from './errors/error';
import { Home } from './pages/home';
import { Login } from './pages/login';
import { Dashboard } from './pages/dashboard';

function App() {
    const router = createBrowserRouter([
        {
            path: "/",
            element: <Root />,
            errorElement: <ErrorPage />,
            children: [
                { index: true, element: <Home /> },
                { path: "Account/Login", element: <Login /> },
                {
                    path: "Dashboard",
                    element:
                        <SecuredRoute>
                            <Dashboard />
                        </SecuredRoute>
                },
                {
                    path: "Admin/Dashboard",
                    element:
                        <SecuredRoute allowedRoles={securityPoints.admin.view}>
                            <Dashboard />
                        </SecuredRoute>
                },
            ]
        }
    ]);

    return (
        <>
            <QueryClientProvider client={queryClientGlobalOptions}>
                <AuthProvider>
                    <RouterProvider router={router} />
                </AuthProvider>
                <ReactQueryDevtools initialIsOpen={false} />
            </QueryClientProvider>
            <ToastBroker.Container />
        </>
    );
}

export default App;
