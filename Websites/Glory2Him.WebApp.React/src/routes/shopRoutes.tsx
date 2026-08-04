import { RouteObject } from 'react-router-dom';

import { Checkout } from '../pages/shop/checkout';
import { EmptyCart } from '../pages/shop/emptyCart';
import { MyCart } from '../pages/shop/myCart';
import { ShopDetail } from '../pages/shop/shopDetail';
import { ShopGrid } from '../pages/shop/shopGrid';

// Shop area routes, mirroring the Blazor @page directives. Mount these under a router
// wrapped in <CartProvider> (see services/views/cart/cartContext.tsx).
export const shopRoutes: RouteObject[] = [
    { path: '/Shop-Grid', element: <ShopGrid /> },
    { path: '/Shop-Detail', element: <ShopDetail /> },
    { path: '/Shop-Detail/:slug', element: <ShopDetail /> },
    { path: '/My-Cart', element: <MyCart /> },
    { path: '/Empty-Cart', element: <EmptyCart /> },
    { path: '/Checkout', element: <Checkout /> },
];
