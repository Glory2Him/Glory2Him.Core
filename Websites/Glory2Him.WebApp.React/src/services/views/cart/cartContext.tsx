import {
    createContext,
    ReactNode,
    useCallback,
    useContext,
    useEffect,
    useMemo,
    useState
} from 'react';

import { ProductView } from '../../../models/coreUI/productView';
import { CartItem, cartItemLineTotal } from '../../../models/views/cart/cartItem';

// Client-side shopping cart, ported from the Blazor demo's per-circuit ICartService /
// CartService. Same operations and semantics: add (merges quantities, clamps to >= 1),
// updateQuantity (removes the line when quantity drops below 1, no-op for unknown ids),
// remove, clear, plus the derived count / subtotal / isEmpty values.
//
// Deviation from Blazor: the Blazor cart lived in server memory for the lifetime of the
// circuit and evaporated on refresh. Here the cart is client state persisted to
// sessionStorage, so a page refresh keeps the cart for the browser session.

const CART_STORAGE_KEY = 'glory2him.cart';

export interface CartContextValue {
    items: ReadonlyArray<CartItem>;
    count: number;
    subtotal: number;
    isEmpty: boolean;
    add: (product: ProductView, quantity?: number) => void;
    updateQuantity: (productId: string, quantity: number) => void;
    remove: (productId: string) => void;
    clear: () => void;
}

const CartContext = createContext<CartContextValue | undefined>(undefined);

const isCartItem = (candidate: unknown): candidate is CartItem => {
    if (typeof candidate !== 'object' || candidate === null) {
        return false;
    }

    const item = candidate as { product?: unknown; quantity?: unknown };

    return typeof item.quantity === 'number'
        && typeof item.product === 'object'
        && item.product !== null
        && typeof (item.product as { id?: unknown }).id === 'string';
};

const readStoredCart = (): CartItem[] => {
    try {
        const storedCart = window.sessionStorage.getItem(CART_STORAGE_KEY);

        if (storedCart === null) {
            return [];
        }

        const parsedCart: unknown = JSON.parse(storedCart);

        if (!Array.isArray(parsedCart)) {
            return [];
        }

        return parsedCart.filter(isCartItem);
    } catch {
        return [];
    }
};

export interface CartProviderProps {
    children: ReactNode;
}

export function CartProvider({ children }: CartProviderProps) {
    const [items, setItems] = useState<CartItem[]>(readStoredCart);

    useEffect(() => {
        try {
            window.sessionStorage.setItem(CART_STORAGE_KEY, JSON.stringify(items));
        } catch {
            // Storage may be unavailable (private mode / quota); the cart still works in memory.
        }
    }, [items]);

    const add = useCallback((product: ProductView, quantity: number = 1) => {
        const quantityToAdd = quantity < 1 ? 1 : quantity;

        setItems((currentItems) => {
            const existingItem =
                currentItems.find((item) => item.product.id === product.id);

            if (existingItem === undefined) {
                return [...currentItems, { product, quantity: quantityToAdd }];
            }

            return currentItems.map((item) =>
                item.product.id === product.id
                    ? { ...item, quantity: item.quantity + quantityToAdd }
                    : item);
        });
    }, []);

    const updateQuantity = useCallback((productId: string, quantity: number) => {
        setItems((currentItems) => {
            const existingItem =
                currentItems.find((item) => item.product.id === productId);

            if (existingItem === undefined) {
                return currentItems;
            }

            if (quantity < 1) {
                return currentItems.filter((item) => item.product.id !== productId);
            }

            return currentItems.map((item) =>
                item.product.id === productId ? { ...item, quantity } : item);
        });
    }, []);

    const remove = useCallback((productId: string) => {
        setItems((currentItems) =>
            currentItems.filter((item) => item.product.id !== productId));
    }, []);

    const clear = useCallback(() => setItems([]), []);

    const contextValue = useMemo<CartContextValue>(() => ({
        items,
        count: items.reduce((total, item) => total + item.quantity, 0),
        subtotal: items.reduce((total, item) => total + cartItemLineTotal(item), 0),
        isEmpty: items.length === 0,
        add,
        updateQuantity,
        remove,
        clear
    }), [items, add, updateQuantity, remove, clear]);

    return (
        <CartContext.Provider value={contextValue}>
            {children}
        </CartContext.Provider>
    );
}

// The provider and its hook belong together; the hook is not a component, so fast
// refresh is unaffected in practice.
// eslint-disable-next-line react-refresh/only-export-components
export function useCart(): CartContextValue {
    const cartContext = useContext(CartContext);

    if (cartContext === undefined) {
        throw new Error('useCart must be used within a CartProvider.');
    }

    return cartContext;
}
