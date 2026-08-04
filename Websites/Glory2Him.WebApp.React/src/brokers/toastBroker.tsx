import { ToastContainer } from 'react-toastify';

const ToastBroker = {
    Container: () => (
        <ToastContainer
            position="top-right"
            autoClose={5000}
            hideProgressBar={false}
            newestOnTop={false}
            closeOnClick
            rtl={false}
            pauseOnFocusLoss
            draggable
            pauseOnHover
        />
    )
};

export default ToastBroker;
