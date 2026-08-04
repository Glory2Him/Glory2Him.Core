import { Link } from 'react-router-dom';
import { ProductView } from '../../models/coreUI/productView';

// Reusable shop product card (Blogzine "Shop-Grid" item). Renders a single ProductView and
// raises onAddToCart.
export interface ProductCardProps {
    product: ProductView;
    onAddToCart?: (product: ProductView) => void;
}

const priceFormatter = new Intl.NumberFormat('en-GB', {
    style: 'currency',
    currency: 'GBP',
});

function starIcon(rating: number, position: number): string {
    if (rating >= position) {
        return 'fa-star';
    }

    return rating >= position - 0.5 ? 'fa-star-half-alt' : 'fa-star';
}

export function ProductCard({ product, onAddToCart }: ProductCardProps) {
    const productHref = `/Shop-Detail/${product.slug}`;

    return (
        <div className="card border p-3 h-100">
            <div className="position-relative">
                <Link to={productHref} className="position-relative z-index-9">
                    <img className="card-img" src={product.imageUrl} alt={product.name} />
                </Link>
                {product.badge != null && product.badge.trim().length > 0 && (
                    <div className="card-img-overlay p-0">
                        <div><span className={`badge ${product.badgeCss}`}>{product.badge}</span></div>
                    </div>
                )}
            </div>

            <div className="card-body text-center p-3 px-0">
                <div className="d-flex justify-content-center mb-2">
                    <ul className="list-inline mb-0">
                        {[1, 2, 3, 4, 5].map((star) => (
                            <li key={star} className="list-inline-item me-0 small">
                                <i className={`fas ${starIcon(product.rating, star)} text-warning`}></i>
                            </li>
                        ))}
                    </ul>
                </div>
                <h5 className="card-title"><Link to={productHref}>{product.name}</Link></h5>
                <h6 className="mb-0 text-success">{priceFormatter.format(product.price)}</h6>
            </div>

            <div className="card-footer text-center p-0">
                <button
                    type="button"
                    className="btn btn-sm btn-primary-soft mb-0"
                    onClick={() => onAddToCart?.(product)}>
                    <i className="bi bi-cart me-2"></i>Add to cart
                </button>
            </div>
        </div>
    );
}
