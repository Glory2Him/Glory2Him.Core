import { useEffect, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { Breadcrumb } from '../components/coreUI/breadcrumb';
import { Card } from '../components/coreUI/card';
import { Chart } from '../components/coreUI/chart';
import { formatDate } from '../components/coreUI/dateFormats';
import { Spinner } from '../components/coreUI/spinner';
import { StatTile } from '../components/coreUI/statTile';
import { BreadcrumbItem } from '../models/coreUI/breadcrumbItem';
import { ChartDataset } from '../models/coreUI/chartDataset';
import { postService } from '../services/foundations/postService';
import { userAdminService } from '../services/foundations/userAdminService';

// Ported from the Blazor Dashboard page: Blogzine dashboard counter row (CoreUI StatTile),
// a posts-by-category ApexCharts donut, an at-a-glance list and the latest posts.

const crumbs: BreadcrumbItem[] = [
    { title: 'Dashboard', href: '/Dashboard', isActive: true },
];

const palette = ['#2163e8', '#0cbc87', '#d6293e', '#f7c32e', '#4f42b5', '#0d6efd'];

export const Dashboard = () => {
    const { data: pagedPosts, isLoading: arePostsLoading, isError: isPostsError } =
        postService.useGetPosts({ page: 1, pageSize: 1000 });

    const { data: users, isLoading: areUsersLoading, isError: isUsersError } =
        userAdminService.useGetAllUsers();

    useEffect(() => {
        document.title = 'Dashboard — Glory 2 Him';
    }, []);

    const posts = useMemo(() => pagedPosts?.items ?? [], [pagedPosts]);

    const isLoading = arePostsLoading || areUsersLoading;
    const hasError = isPostsError || isUsersError;

    const postCount = posts.length;
    const userCount = users?.length ?? 0;
    const featuredCount = posts.filter((post) => post.isFeatured).length;

    const categoryCount = useMemo(
        () => new Set(posts.map((post) => post.category)).size,
        [posts]);

    // Posts-per-category, shaped for the CoreUI Chart (ApexCharts donut).
    const { categoryLabels, categoryDatasets } = useMemo(() => {
        const counts = new Map<string, number>();

        posts.forEach((post) =>
            counts.set(post.category, (counts.get(post.category) ?? 0) + 1));

        const grouped = [...counts.entries()]
            .map(([category, count]) => ({ category, count }))
            .sort((left, right) => right.count - left.count);

        const datasets: ChartDataset[] = [
            {
                label: 'Posts',
                data: grouped.map((entry) => entry.count),
                colors: grouped.map((_, index) => palette[index % palette.length]),
            },
        ];

        return {
            categoryLabels: grouped.map((entry) => entry.category),
            categoryDatasets: datasets,
        };
    }, [posts]);

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">Dashboard</h1>
                <Breadcrumb items={crumbs} />
            </div>
            <hr />

            {isLoading ? (
                <div className="text-center py-5">
                    <Spinner />
                </div>
            ) : hasError ? (
                <div className="alert alert-danger" role="alert">
                    We could not load the dashboard right now. Please try again later.
                </div>
            ) : (
                <>
                    {/* Counters (Blogzine dashboard counter row, powered by the CoreUI StatTile) */}
                    <div className="row g-4 mb-4">
                        <div className="col-sm-6 col-lg-3">
                            <StatTile variant="Green" icon="bi-file-earmark-text-fill"
                                value={String(postCount)} label="Posts" />
                        </div>
                        <div className="col-sm-6 col-lg-3">
                            <StatTile variant="Na" icon="bi-people-fill"
                                value={String(userCount)} label="Users" />
                        </div>
                        <div className="col-sm-6 col-lg-3">
                            <StatTile variant="Amber" icon="bi-star-fill"
                                value={String(featuredCount)} label="Featured" />
                        </div>
                        <div className="col-sm-6 col-lg-3">
                            <StatTile variant="Red" icon="bi-suit-heart-fill"
                                value={String(categoryCount)} label="Categories" />
                        </div>
                    </div>

                    <div className="row g-4 mb-4">
                        <div className="col-xl-7">
                            <Card title="Posts by category">
                                <Chart chartType="donut" labels={categoryLabels}
                                    datasets={categoryDatasets} height={300} />
                            </Card>
                        </div>
                        <div className="col-xl-5">
                            <Card title="At a glance">
                                <ul className="list-group list-group-flush">
                                    <li className="list-group-item d-flex justify-content-between px-0">
                                        <span>Total posts</span><strong>{postCount}</strong>
                                    </li>
                                    <li className="list-group-item d-flex justify-content-between px-0">
                                        <span>Featured posts</span><strong>{featuredCount}</strong>
                                    </li>
                                    <li className="list-group-item d-flex justify-content-between px-0">
                                        <span>Categories</span><strong>{categoryCount}</strong>
                                    </li>
                                    <li className="list-group-item d-flex justify-content-between px-0">
                                        <span>Registered users</span><strong>{userCount}</strong>
                                    </li>
                                </ul>
                            </Card>
                        </div>
                    </div>

                    {/* Latest posts */}
                    <Card title="Latest posts">
                        {posts.length === 0 ? (
                            <div className="alert alert-info mb-0" role="alert">
                                No posts have been published yet.
                            </div>
                        ) : (
                            <div className="list-group list-group-flush">
                                {posts.slice(0, 5).map((post) => (
                                    <div key={post.id} className="list-group-item d-flex align-items-center px-0">
                                        <img className="w-60 rounded" src={post.imageUrl} alt={post.title}
                                            style={{ maxWidth: '60px' }} />
                                        <div className="ms-3">
                                            <Link to={`/Post-Single/${post.slug}`} className="h6 mb-0 d-block">
                                                {post.title}
                                            </Link>
                                            <p className="small mb-0 text-body-secondary">
                                                {formatDate(new Date(post.publishedDate))} · {post.category}
                                            </p>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )}
                    </Card>
                </>
            )}
        </>
    );
};
