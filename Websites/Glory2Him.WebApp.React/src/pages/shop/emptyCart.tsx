import { useEffect } from 'react';
import { Link } from 'react-router-dom';

// Standalone empty-cart page, ported from the Blazor EmptyCart.razor.
export function EmptyCart() {
    useEffect(() => {
        document.title = 'Cart empty — Glory 2 Him';
    }, []);

    return (
        <section className="position-relative overflow-hidden py-5">
            <div className="container">
                <div className="row justify-content-center text-center py-5">
                    <div className="col-lg-7">
                        <i className="bi bi-cart-x display-1 text-body-secondary"></i>
                        <h1 className="mt-3">Your cart is empty</h1>
                        <p className="mb-4">
                            Looks like you have not added anything to your cart yet. Browse our resources
                            and find something to encourage you.
                        </p>
                        <Link to="/Shop-Grid" className="btn btn-primary mb-0">Start shopping</Link>
                    </div>
                </div>
            </div>
        </section>
    );
}
