import { ReactNode } from 'react';
import { act, renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import { ProductView } from '../../../models/coreUI/productView';
import { CartProvider, useCart } from './cartContext';

const CART_STORAGE_KEY = 'glory2him.cart';

const createProduct = (overrides: Partial<ProductView> = {}): ProductView => ({
    id: 'product-1',
    name: 'Study Bible',
    slug: 'study-bible',
    description: 'A study Bible.',
    imageUrl: '/assets/study-bible.jpg',
    price: 25,
    rating: 5,
    badgeCss: 'bg-primary',
    ...overrides
});

const wrapper = ({ children }: { children: ReactNode }) =>
    <CartProvider>{children}</CartProvider>;

const renderCart = () => renderHook(() => useCart(), { wrapper });

describe('CartProvider', () => {
    beforeEach(() => {
        window.sessionStorage.clear();
    });

    it('should start empty', () => {
        // when
        const { result } = renderCart();

        // then
        expect(result.current.items).toHaveLength(0);
        expect(result.current.count).toBe(0);
        expect(result.current.subtotal).toBe(0);
        expect(result.current.isEmpty).toBe(true);
    });

    it('should add a product with a default quantity of one', () => {
        // given
        const product = createProduct();
        const { result } = renderCart();

        // when
        act(() => result.current.add(product));

        // then
        expect(result.current.items).toEqual([{ product, quantity: 1 }]);
        expect(result.current.count).toBe(1);
        expect(result.current.isEmpty).toBe(false);
    });

    it('should merge quantities when adding the same product again', () => {
        // given
        const product = createProduct();
        const { result } = renderCart();

        // when
        act(() => result.current.add(product, 2));
        act(() => result.current.add(product, 3));

        // then
        expect(result.current.items).toHaveLength(1);
        expect(result.current.items[0].quantity).toBe(5);
        expect(result.current.count).toBe(5);
    });

    it('should clamp an add quantity below one up to one', () => {
        // given
        const product = createProduct();
        const { result } = renderCart();

        // when
        act(() => result.current.add(product, 0));

        // then
        expect(result.current.items[0].quantity).toBe(1);
    });

    it('should update the quantity of an existing line', () => {
        // given
        const product = createProduct();
        const { result } = renderCart();
        act(() => result.current.add(product));

        // when
        act(() => result.current.updateQuantity(product.id, 4));

        // then
        expect(result.current.items[0].quantity).toBe(4);
        expect(result.current.count).toBe(4);
    });

    it('should remove the line when the quantity drops below one', () => {
        // given
        const product = createProduct();
        const { result } = renderCart();
        act(() => result.current.add(product, 2));

        // when
        act(() => result.current.updateQuantity(product.id, 0));

        // then
        expect(result.current.items).toHaveLength(0);
        expect(result.current.isEmpty).toBe(true);
    });

    it('should ignore quantity updates for unknown product ids', () => {
        // given
        const product = createProduct();
        const { result } = renderCart();
        act(() => result.current.add(product, 2));

        // when
        act(() => result.current.updateQuantity('unknown-id', 9));

        // then
        expect(result.current.items).toEqual([{ product, quantity: 2 }]);
    });

    it('should remove only the requested line', () => {
        // given
        const firstProduct = createProduct();
        const secondProduct = createProduct({ id: 'product-2', name: 'Hymnal', price: 10 });
        const { result } = renderCart();
        act(() => result.current.add(firstProduct));
        act(() => result.current.add(secondProduct));

        // when
        act(() => result.current.remove(firstProduct.id));

        // then
        expect(result.current.items).toEqual([{ product: secondProduct, quantity: 1 }]);
    });

    it('should clear all lines', () => {
        // given
        const { result } = renderCart();
        act(() => result.current.add(createProduct()));
        act(() => result.current.add(createProduct({ id: 'product-2' })));

        // when
        act(() => result.current.clear());

        // then
        expect(result.current.items).toHaveLength(0);
        expect(result.current.isEmpty).toBe(true);
    });

    it('should compute the subtotal across lines', () => {
        // given
        const firstProduct = createProduct({ price: 25 });
        const secondProduct = createProduct({ id: 'product-2', price: 10 });
        const { result } = renderCart();

        // when
        act(() => result.current.add(firstProduct, 2));
        act(() => result.current.add(secondProduct, 3));

        // then
        expect(result.current.subtotal).toBe(25 * 2 + 10 * 3);
    });

    it('should persist the cart to sessionStorage and restore it in a new provider', () => {
        // given
        const product = createProduct();
        const firstRender = renderCart();
        act(() => firstRender.result.current.add(product, 2));
        firstRender.unmount();

        // when
        const secondRender = renderCart();

        // then
        expect(secondRender.result.current.items).toEqual([{ product, quantity: 2 }]);
        expect(secondRender.result.current.count).toBe(2);
    });

    it('should ignore malformed sessionStorage content', () => {
        // given
        window.sessionStorage.setItem(CART_STORAGE_KEY, 'not-json{');

        // when
        const { result } = renderCart();

        // then
        expect(result.current.items).toHaveLength(0);
    });

    it('should drop stored entries that are not cart items', () => {
        // given
        const product = createProduct();
        window.sessionStorage.setItem(
            CART_STORAGE_KEY,
            JSON.stringify([{ product, quantity: 2 }, { bogus: true }, 42]));

        // when
        const { result } = renderCart();

        // then
        expect(result.current.items).toEqual([{ product, quantity: 2 }]);
    });

    it('should throw when useCart is used outside a CartProvider', () => {
        // when / then
        expect(() => renderHook(() => useCart()))
            .toThrowError('useCart must be used within a CartProvider.');
    });
});
