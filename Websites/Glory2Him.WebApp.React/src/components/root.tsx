import { Outlet } from "react-router-dom";
import HeaderComponent from "./layouts/header";
import FooterComponent from "./layouts/footer";

export default function Root() {
    return (
        <>
            <HeaderComponent />
            <main>
                <Outlet />
            </main>
            <FooterComponent />
        </>
    );
}
