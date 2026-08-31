import { useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Button } from '../../components/coreUI/button';
import { Card } from '../../components/coreUI/card';
import { DataTable } from '../../components/coreUI/dataTable';
import { formatDate } from '../../components/coreUI/dateFormats';
import { Spinner } from '../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';
import { DataTableColumn } from '../../models/coreUI/dataTableColumn';
import { PostView } from '../../models/coreUI/postView';
import { postService } from '../../services/foundations/postService';

// Ported from the Blazor Admin/PostsPage: the DataTable sorts, pages and searches client-side.

const crumbs: BreadcrumbItem[] = [
    { title: 'Admin' },
    { title: 'Sample Posts', href: '/Admin/SamplePosts', isActive: true },
];

const columns: DataTableColumn<PostView>[] = [
    {
        title: 'Title',
        value: (post) => post.title,
    },
    {
        title: 'Category',
        value: (post) => post.category,
        cellTemplate: (post) =>
            <span className={`badge ${post.categoryBadgeCss}`}>{post.category}</span>,
    },
    {
        title: 'Author',
        value: (post) => post.authorName,
    },
    {
        title: 'Published',
        value: (post) => new Date(post.publishedDate),
        cellTemplate: (post) =>
            <span>{formatDate(new Date(post.publishedDate))}</span>,
    },
];

export const PostsPage = () => {
    const navigate = useNavigate();

    // The admin list works over the full catalogue, like the Blazor page's RetrieveAllPostsAsync.
    const { data: pagedPosts, isLoading, isError } =
        postService.useGetPosts({ page: 1, pageSize: 1000 });

    const posts = pagedPosts?.items;

    useEffect(() => {
        document.title = 'Posts — Glory 2 Him';
    }, []);

    // Creating and editing both happen on their own addressable page, so the list only routes.
    const createPost = () => navigate('/Admin/SamplePosts/New');
    const editPost = (postId: string) => navigate(`/Admin/SamplePosts/${postId}`);

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">Posts</h1>
                <Breadcrumb items={crumbs} />
            </div>
            <hr />

            <div className="d-flex justify-content-end mb-3">
                <Button color="primary" onClick={createPost}>
                    <i className="bi bi-plus-lg me-1"></i>New post
                </Button>
            </div>

            {isLoading ? (
                <div className="text-center py-5">
                    <Spinner />
                </div>
            ) : isError ? (
                <div className="alert alert-danger" role="alert">
                    We could not load posts right now. Please try again later.
                </div>
            ) : (posts == null || posts.length === 0) ? (
                <div className="alert alert-info" role="alert">
                    No posts found. Create one to get started.
                </div>
            ) : (
                <Card>
                    <DataTable
                        items={posts}
                        columns={columns}
                        pageSize={10}
                        rowActions={(post) => (
                            <>
                                <Link className="btn btn-sm btn-outline-secondary" to={`/Post-Single/${post.slug}`}>
                                    View
                                </Link>
                                <Button color="outline-primary" cssClass="btn-sm" onClick={() => editPost(post.id)}>
                                    Manage
                                </Button>
                            </>
                        )} />
                </Card>
            )}
        </>
    );
};
