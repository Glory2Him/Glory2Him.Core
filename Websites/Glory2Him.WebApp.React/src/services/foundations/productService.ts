import { useQuery } from '@tanstack/react-query';
import ProductBroker from "../../brokers/apiBroker.products";
import { ProductView } from "../../models/coreUI/productView";

export const productService = {
    useGetAllProducts: () => {
        const productBroker = new ProductBroker();

        return useQuery<ProductView[]>({
            queryKey: ["ProductsGetAll"],
            queryFn: async () => await productBroker.GetAllProductsAsync(),
            staleTime: 5 * 60 * 1000
        });
    },

    useGetProductBySlug: (slug: string) => {
        const productBroker = new ProductBroker();

        return useQuery<ProductView>({
            queryKey: ["ProductsGetBySlug", slug],
            queryFn: async () => await productBroker.GetProductBySlugAsync(slug),
            staleTime: 5 * 60 * 1000
        });
    }
};
