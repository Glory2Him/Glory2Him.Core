import { ProductView } from "../models/coreUI/productView";
import ApiBroker from "./apiBroker";

class ProductBroker {
    relativeProductsUrl = '/api/products';
    private apiBroker: ApiBroker = new ApiBroker();

    async GetAllProductsAsync(): Promise<ProductView[]> {
        const result = await this.apiBroker.GetAsync(this.relativeProductsUrl);

        return result.data as ProductView[];
    }

    async GetProductBySlugAsync(slug: string): Promise<ProductView> {
        const url = `${this.relativeProductsUrl}/slug/${encodeURIComponent(slug)}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as ProductView;
    }
}

export default ProductBroker;
