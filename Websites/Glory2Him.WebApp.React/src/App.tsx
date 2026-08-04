import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import "react-toastify/dist/ReactToastify.css";
import { queryClientGlobalOptions } from './brokers/apiBroker.globals';
import ToastBroker from './brokers/toastBroker';
import { AuthProvider } from './components/securitys/authProvider';
import { CartProvider } from './services/views/cart/cartContext';
import Root from './components/root';
import ErrorPage from './errors/error';
import { publicPostRoutes } from './routes/publicPostRoutes';
import { staticRoutes } from './routes/staticRoutes';
import { shopRoutes } from './routes/shopRoutes';
import { adminRoutes } from './routes/adminRoutes';
import { accountRoutes } from './routes/accountRoutes';
import { passkeyRoutes } from './routes/passkeyRoutes';
import { samplePagesRoutes } from './routes/samplePagesRoutes';

// Route order matters only for the staticRoutes catch-all ("*" → NotFound), which must be
// the very last child — staticRoutes exports it last, so staticRoutes stays last here.
function App() {
    const router = createBrowserRouter([
        {
            path: "/",
            element: <Root />,
            errorElement: <ErrorPage />,
            children: [
                ...publicPostRoutes,
                ...shopRoutes,
                ...adminRoutes,
                ...accountRoutes,
                ...passkeyRoutes,
                ...samplePagesRoutes,
                ...staticRoutes,
            ]
        }
    ]);

    return (
        <>
            <QueryClientProvider client={queryClientGlobalOptions}>
                <AuthProvider>
                    <CartProvider>
                        <RouterProvider router={router} />
                    </CartProvider>
                </AuthProvider>
                <ReactQueryDevtools initialIsOpen={false} />
            </QueryClientProvider>
            <ToastBroker.Container />
        </>
    );
}

export default App;
