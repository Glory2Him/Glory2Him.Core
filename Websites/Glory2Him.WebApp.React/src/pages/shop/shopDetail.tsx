import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';

import { Spinner } from '../../components/coreUI/spinner';
import { productService } from '../../services/foundations/productService';
import { useCart } from '../../services/views/cart/cartContext';

// Product detail page, ported from the Blazor ShopDetail.razor / ShopDetailBase.cs.
const priceFormatter = new Intl.NumberFormat('en-GB', {
    style: 'currency',
    currency: 'GBP',
});

function starIcon(rating: number, star: number): string {
    if (rating >= star) {
        return 'fa-star';
    }

    return rating >= star - 0.5 ? 'fa-star-half-alt' : 'fa-star';
}

export function ShopDetail() {
    const { slug } = useParams<{ slug: string }>();
    const { data: product, isLoading, isError } =
        productService.useGetProductBySlug(slug ?? '');

    const { add } = useCart();
    const [quantity, setQuantity] = useState(1);
    const [wasAdded, setWasAdded] = useState(false);

    useEffect(() => {
        document.title = `${product?.name ?? 'Product'} — Glory 2 Him`;
    }, [product]);

    useEffect(() => {
        setQuantity(1);
        setWasAdded(false);
    }, [slug]);

    const addToCart = () => {
        if (product === undefined) {
            return;
        }

        add(product, quantity);
        setWasAdded(true);
    };

    if (isLoading) {
        return (
            <section className="py-5">
                <div className="container text-center"><Spinner /></div>
            </section>
        );
    }

    if (isError) {
        return (
            <section className="py-5">
                <div className="container">
                    <div className="alert alert-danger mb-0">
                        We could not load this product right now. Please try again later.
                    </div>
                </div>
            </section>
        );
    }

    if (product === undefined) {
        return (
            <section className="py-5">
                <div className="container">
                    <div className="alert alert-info mb-0">This product could not be found.</div>
                </div>
            </section>
        );
    }

    return (
        <section className="pt-4 pb-5">
            <div className="container">
                <nav aria-label="breadcrumb" className="mb-3">
                    <ol className="breadcrumb mb-0">
                        <li className="breadcrumb-item"><Link to="/">Home</Link></li>
                        <li className="breadcrumb-item"><Link to="/Shop-Grid">Shop</Link></li>
                        <li className="breadcrumb-item active" aria-current="page">{product.name}</li>
                    </ol>
                </nav>

                <div className="row g-4">
                    <div className="col-md-6">
                        <img className="rounded-3 w-100" src={product.imageUrl} alt={product.name} />
                    </div>
                    <div className="col-md-6">
                        {product.badge != null && product.badge.trim().length > 0 && (
                            <span className={`badge ${product.badgeCss} mb-2`}>{product.badge}</span>
                        )}
                        <h1 className="h2">{product.name}</h1>
                        <div className="mb-2">
                            {[1, 2, 3, 4, 5].map((star) => (
                                <i
                                    key={star}
                                    className={`fas ${starIcon(product.rating, star)} text-warning small`}></i>
                            ))}
                            <span className="ms-2 small text-body-secondary">
                                {product.rating.toFixed(1)}
                            </span>
                        </div>
                        <h3 className="text-success mb-3">{priceFormatter.format(product.price)}</h3>
                        <p>{product.description}</p>

                        {wasAdded && (
                            <div className="alert alert-success py-2" role="alert">
                                Added to your cart. <Link to="/My-Cart" className="alert-link">View cart</Link>.
                            </div>
                        )}

                        <div className="d-flex align-items-center gap-3 mt-3">
                            <div className="input-group" style={{ maxWidth: 140 }}>
                                <button
                                    className="btn btn-outline-secondary"
                                    type="button"
                                    onClick={() => setQuantity((current) => Math.max(1, current - 1))}>
                                    -
                                </button>
                                <input
                                    className="form-control text-center"
                                    type="text"
                                    value={quantity}
                                    readOnly />
                                <button
                                    className="btn btn-outline-secondary"
                                    type="button"
                                    onClick={() => setQuantity((current) => current + 1)}>
                                    +
                                </button>
                            </div>
                            <button className="btn btn-primary mb-0" onClick={addToCart}>
                                <i className="bi bi-cart me-2"></i>Add to cart
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
