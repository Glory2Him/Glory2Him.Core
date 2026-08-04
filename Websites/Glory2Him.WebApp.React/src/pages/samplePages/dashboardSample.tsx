import { Card } from '../../components/coreUI/card';
import { Chart } from '../../components/coreUI/chart';
import { DataTable } from '../../components/coreUI/dataTable';
import { formatDate } from '../../components/coreUI/dateFormats';
import { HeroBanner } from '../../components/coreUI/heroBanner';
import { Spinner } from '../../components/coreUI/spinner';
import { StatTile } from '../../components/coreUI/statTile';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';
import { ChartDataset } from '../../models/coreUI/chartDataset';
import { DataTableColumn } from '../../models/coreUI/dataTableColumn';
import { PostView } from '../../models/coreUI/postView';
import { useDocumentTitle } from '../useDocumentTitle';
import { SampleShell } from './shared/sampleShell';
import { useSamplePosts } from './shared/useSamplePosts';

// Blogzine dashboard.html: the author dashboard shown full width inside the site chrome
// rather than the admin shell.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'Dashboard', isActive: true },
];

const dayLabels: ReadonlyArray<string> =
    ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

const viewDatasets: ReadonlyArray<ChartDataset> = [
    {
        label: 'Views',
        data: [820, 932, 901, 1290, 1330, 1120, 1420],
    },
];

const columns: ReadonlyArray<DataTableColumn<PostView>> = [
    { title: 'Title', value: (post) => post.title },
    {
        title: 'Category',
        value: (post) => post.category,
        cellTemplate: (post) =>
            <span className={`badge ${post.categoryBadgeCss}`}>{post.category}</span>,
    },
    { title: 'Author', value: (post) => post.authorName },
    {
        title: 'Published',
        value: (post) => post.publishedDate,
        cellTemplate: (post) =>
            <span>{formatDate(post.publishedDate)}</span>,
    },
];

export const DashboardSample = () => {
    useDocumentTitle('Dashboard — Sample — Glory 2 Him');

    const { posts, isLoading, isError } = useSamplePosts();

    const featuredCount = posts.filter((post) => post.isFeatured).length;

    const categoryLabels = [...new Map(
        posts.map((post) => [post.category.toLowerCase(), post.category]),
    ).values()].sort((left, right) => left.localeCompare(right));

    const categoryDatasets: ReadonlyArray<ChartDataset> = [
        {
            label: 'Posts',
            data: categoryLabels.map((category) =>
                posts.filter((post) =>
                    post.category.toLowerCase() === category.toLowerCase()).length),
        },
    ];

    return (
        <SampleShell title="Dashboard" sourceFile="dashboard.html">
            <HeroBanner title="Author dashboard" crumbs={crumbs} />

            <section className="py-5">
                <div className="container">
                    {isLoading ? (
                        <div className="text-center py-5"><Spinner /></div>
                    ) : isError ? (
                        <div className="alert alert-danger" role="alert">
                            We could not load posts right now. Please try again later.
                        </div>
                    ) : (
                        <>
                            <div className="row g-4 mb-4">
                                <div className="col-sm-6 col-lg-3">
                                    <StatTile
                                        variant="Green"
                                        icon="bi-file-earmark-text-fill"
                                        value={String(posts.length)}
                                        label="Posts" />
                                </div>
                                <div className="col-sm-6 col-lg-3">
                                    <StatTile variant="Na" icon="bi-eye-fill" value="8.2k" label="Views" />
                                </div>
                                <div className="col-sm-6 col-lg-3">
                                    <StatTile
                                        variant="Amber"
                                        icon="bi-star-fill"
                                        value={String(featuredCount)}
                                        label="Featured" />
                                </div>
                                <div className="col-sm-6 col-lg-3">
                                    <StatTile
                                        variant="Red"
                                        icon="bi-chat-heart-fill"
                                        value="126"
                                        label="Comments" />
                                </div>
                            </div>

                            <div className="row g-4 mb-4">
                                <div className="col-xl-7">
                                    <Card title="Views this week">
                                        <Chart
                                            chartType="area"
                                            labels={dayLabels}
                                            datasets={viewDatasets}
                                            height={300} />
                                    </Card>
                                </div>

                                <div className="col-xl-5">
                                    <Card title="Posts by category">
                                        <Chart
                                            chartType="donut"
                                            labels={categoryLabels}
                                            datasets={categoryDatasets}
                                            height={300} />
                                    </Card>
                                </div>
                            </div>

                            <Card title="Recent posts">
                                <DataTable<PostView> items={posts} columns={columns} pageSize={5} />
                            </Card>
                        </>
                    )}
                </div>
            </section>
        </SampleShell>
    );
};
