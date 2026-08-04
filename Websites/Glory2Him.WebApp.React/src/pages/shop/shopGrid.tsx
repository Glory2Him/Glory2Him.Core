import { useEffect } from 'react';
import { Link } from 'react-router-dom';

import { PageHeader } from '../../components/coreUI/pageHeader';
import { ProductCard } from '../../components/coreUI/productCard';
import { Spinner } from '../../components/coreUI/spinner';
import { ProductView } from '../../models/coreUI/productView';
import { productService } from '../../services/foundations/productService';
import { useCart } from '../../services/views/cart/cartContext';

// Shop landing page, ported from the Blazor ShopGrid.razor / ShopGridBase.cs.
export function ShopGrid() {
    const { data: products, isLoading, isError } = productService.useGetAllProducts();
    const { count, add } = useCart();

    useEffect(() => {
        document.title = 'Shop — Glory 2 Him';
    }, []);

    const addToCart = (product: ProductView) => add(product);

    return (
        <>
            <PageHeader title="Shop" />

            <section className="pt-4 pb-5">
                <div className="container">
                    <div className="d-flex justify-content-between align-items-center mb-4">
                        <h2 className="h4 mb-0">Resources</h2>
                        <Link to="/My-Cart" className="btn btn-sm btn-primary-soft">
                            <i className="bi bi-cart me-2"></i>Cart ({count})
                        </Link>
                    </div>

                    {isLoading ? (
                        <div className="text-center py-5"><Spinner /></div>
                    ) : isError ? (
                        <div className="alert alert-danger" role="alert">
                            We could not load products right now. Please try again later.
                        </div>
                    ) : products === undefined || products.length === 0 ? (
                        <div className="alert alert-info" role="alert">No products available.</div>
                    ) : (
                        <div className="row g-4">
                            {products.map((product) => (
                                <div key={product.id} className="col-sm-6 col-md-4">
                                    <ProductCard product={product} onAddToCart={addToCart} />
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </section>
        </>
    );
}
