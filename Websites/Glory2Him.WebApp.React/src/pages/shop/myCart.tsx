import { useEffect } from 'react';
import { Link } from 'react-router-dom';

import { PageHeader } from '../../components/coreUI/pageHeader';
import { cartItemLineTotal } from '../../models/views/cart/cartItem';
import { useCart } from '../../services/views/cart/cartContext';

// Cart page, ported from the Blazor MyCart.razor.
const priceFormatter = new Intl.NumberFormat('en-GB', {
    style: 'currency',
    currency: 'GBP',
});

export function MyCart() {
    const { items, count, subtotal, isEmpty, updateQuantity, remove, clear } = useCart();

    useEffect(() => {
        document.title = 'My cart — Glory 2 Him';
    }, []);

    return (
        <>
            <PageHeader title="My cart" parentTitle="Shop" parentHref="/Shop-Grid" />

            <section className="pt-4 pb-5">
                <div className="container">
                    {isEmpty ? (
                        <div className="text-center py-5">
                            <i className="bi bi-cart-x display-1 text-body-secondary"></i>
                            <h2 className="mt-3">Your cart is empty</h2>
                            <p className="mb-4">
                                Looks like you have not added anything to your cart yet.
                            </p>
                            <Link to="/Shop-Grid" className="btn btn-primary">Continue shopping</Link>
                        </div>
                    ) : (
                        <div className="row g-4">
                            <div className="col-lg-8">
                                <div className="table-responsive">
                                    <table className="table align-middle">
                                        <thead>
                                            <tr>
                                                <th>Product</th>
                                                <th>Price</th>
                                                <th style={{ width: 150 }}>Quantity</th>
                                                <th className="text-end">Total</th>
                                                <th></th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {items.map((item) => (
                                                <tr key={item.product.id}>
                                                    <td>
                                                        <div className="d-flex align-items-center">
                                                            <img
                                                                src={item.product.imageUrl}
                                                                alt={item.product.name}
                                                                style={{ width: 56 }}
                                                                className="rounded me-3" />
                                                            <Link
                                                                to={`/Shop-Detail/${item.product.slug}`}
                                                                className="h6 mb-0">
                                                                {item.product.name}
                                                            </Link>
                                                        </div>
                                                    </td>
                                                    <td>{priceFormatter.format(item.product.price)}</td>
                                                    <td>
                                                        <div
                                                            className="input-group input-group-sm"
                                                            style={{ maxWidth: 120 }}>
                                                            <button
                                                                className="btn btn-outline-secondary"
                                                                onClick={() => updateQuantity(
                                                                    item.product.id,
                                                                    item.quantity - 1)}>
                                                                -
                                                            </button>
                                                            <input
                                                                className="form-control text-center"
                                                                type="text"
                                                                value={item.quantity}
                                                                readOnly />
                                                            <button
                                                                className="btn btn-outline-secondary"
                                                                onClick={() => updateQuantity(
                                                                    item.product.id,
                                                                    item.quantity + 1)}>
                                                                +
                                                            </button>
                                                        </div>
                                                    </td>
                                                    <td className="text-end">
                                                        {priceFormatter.format(cartItemLineTotal(item))}
                                                    </td>
                                                    <td className="text-end">
                                                        <button
                                                            className="btn btn-sm btn-outline-danger"
                                                            onClick={() => remove(item.product.id)}>
                                                            <i className="bi bi-trash"></i>
                                                        </button>
                                                    </td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>
                                <button className="btn btn-link text-danger p-0" onClick={clear}>
                                    Clear cart
                                </button>
                            </div>

                            <div className="col-lg-4">
                                <div className="card card-body border">
                                    <h5 className="mb-3">Order summary</h5>
                                    <div className="d-flex justify-content-between mb-2">
                                        <span>Subtotal ({count} items)</span>
                                        <strong>{priceFormatter.format(subtotal)}</strong>
                                    </div>
                                    <div className="d-flex justify-content-between mb-3 text-body-secondary">
                                        <span>Shipping</span><span>Free</span>
                                    </div>
                                    <hr />
                                    <div className="d-flex justify-content-between mb-3">
                                        <span className="h6 mb-0">Total</span>
                                        <span className="h6 mb-0">{priceFormatter.format(subtotal)}</span>
                                    </div>
                                    <Link to="/Checkout" className="btn btn-primary">
                                        Proceed to checkout
                                    </Link>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            </section>
        </>
    );
}
