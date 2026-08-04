export interface ProductView {
    id: string;
    name: string;
    slug: string;
    description: string;
    imageUrl: string;
    price: number;
    rating: number;
    badge?: string;
    badgeCss: string;
}
