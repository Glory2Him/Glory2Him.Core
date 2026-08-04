import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';

import { PageHeader } from '../../components/coreUI/pageHeader';
import { cartItemLineTotal } from '../../models/views/cart/cartItem';
import { useCart } from '../../services/views/cart/cartContext';

// Demo checkout page, ported from the Blazor Checkout.razor. "Place order" clears the
// cart — no payment is taken.
const priceFormatter = new Intl.NumberFormat('en-GB', {
    style: 'currency',
    currency: 'GBP',
});

export function Checkout() {
    const { items, subtotal, isEmpty, clear } = useCart();
    const [orderPlaced, setOrderPlaced] = useState(false);

    useEffect(() => {
        document.title = 'Checkout — Glory 2 Him';
    }, []);

    const placeOrder = () => {
        setOrderPlaced(true);
        clear();
    };

    return (
        <>
            <PageHeader title="Checkout" parentTitle="Shop" parentHref="/Shop-Grid" />

            <section className="pt-4 pb-5">
                <div className="container">
                    {orderPlaced ? (
                        <div className="text-center py-5">
                            <i className="bi bi-check-circle display-1 text-success"></i>
                            <h2 className="mt-3">Thank you!</h2>
                            <p className="mb-4">
                                This is a demo checkout — no payment was taken. In a real store your order
                                would be confirmed here.
                            </p>
                            <Link to="/Shop-Grid" className="btn btn-primary">Continue shopping</Link>
                        </div>
                    ) : isEmpty ? (
                        <div className="alert alert-info" role="alert">
                            Your cart is empty. <Link to="/Shop-Grid" className="alert-link">Browse the shop</Link> first.
                        </div>
                    ) : (
                        <div className="row g-4">
                            <div className="col-lg-7">
                                <div className="card card-body border">
                                    <h5 className="mb-3">Billing details</h5>
                                    <div className="row g-3">
                                        <div className="col-md-6">
                                            <label className="form-label" htmlFor="co-first">First name</label>
                                            <input className="form-control" id="co-first" placeholder="First name" />
                                        </div>
                                        <div className="col-md-6">
                                            <label className="form-label" htmlFor="co-last">Last name</label>
                                            <input className="form-control" id="co-last" placeholder="Last name" />
                                        </div>
                                        <div className="col-12">
                                            <label className="form-label" htmlFor="co-email">Email</label>
                                            <input type="email" className="form-control" id="co-email" placeholder="you@example.com" />
                                        </div>
                                        <div className="col-12">
                                            <label className="form-label" htmlFor="co-address">Address</label>
                                            <input className="form-control" id="co-address" placeholder="Street address" />
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div className="col-lg-5">
                                <div className="card card-body border">
                                    <h5 className="mb-3">Your order</h5>
                                    {items.map((item) => (
                                        <div
                                            key={item.product.id}
                                            className="d-flex justify-content-between mb-2">
                                            <span>{item.product.name} × {item.quantity}</span>
                                            <span>{priceFormatter.format(cartItemLineTotal(item))}</span>
                                        </div>
                                    ))}
                                    <hr />
                                    <div className="d-flex justify-content-between mb-3">
                                        <span className="h6 mb-0">Total</span>
                                        <span className="h6 mb-0">{priceFormatter.format(subtotal)}</span>
                                    </div>
                                    <button className="btn btn-primary" onClick={placeOrder}>Place order</button>
                                    <p className="small text-body-secondary mt-2 mb-0">
                                        Demo only — this will not charge any payment method.
                                    </p>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            </section>
        </>
    );
}
