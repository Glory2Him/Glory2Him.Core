import { ProductView } from '../../coreUI/productView';

// One line in the shopping cart: a product plus how many of it the visitor wants.
// Mirrors the Blazor demo's Services/Cart/CartItem.cs.
export interface CartItem {
    product: ProductView;
    quantity: number;
}

export const cartItemLineTotal = (item: CartItem): number =>
    item.product.price * item.quantity;
