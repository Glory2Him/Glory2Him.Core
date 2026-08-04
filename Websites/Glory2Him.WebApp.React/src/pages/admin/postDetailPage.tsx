import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Button } from '../../components/coreUI/button';
import { Card } from '../../components/coreUI/card';
import { ConfirmDialog } from '../../components/coreUI/confirmDialog';
import { FormDate } from '../../components/coreUI/formDate';
import { FormSelect } from '../../components/coreUI/formSelect';
import { FormSwitch } from '../../components/coreUI/formSwitch';
import { FormText } from '../../components/coreUI/formText';
import { Spinner } from '../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';
import { PostView } from '../../models/coreUI/postView';
import { SelectOption } from '../../models/coreUI/selectOption';
import { postService } from '../../services/foundations/postService';
import { extractApiErrorMessage } from './apiErrorMessage';

// Ported from the Blazor Admin/PostDetailPage: the editor binds field-by-field. The "New" route
// renders the same page without a post id, which is what tells it that it is creating.

const postsRoute = '/Admin/Posts';

const badgeOptions: SelectOption[] = [
    { value: 'text-bg-primary', text: 'Primary' },
    { value: 'text-bg-success', text: 'Success' },
    { value: 'text-bg-warning', text: 'Warning' },
    { value: 'text-bg-danger', text: 'Danger' },
    { value: 'text-bg-info', text: 'Info' },
];

// Sensible starting values so a new post renders like a real one straight away.
function createDraft(): PostView {
    return {
        id: '',
        title: '',
        slug: '',
        excerpt: '',
        imageUrl: 'assets/images/blog/16by9/big/01.jpg',
        category: 'Faith',
        categoryBadgeCss: 'text-bg-primary',
        authorName: 'Glory 2 Him',
        authorImageUrl: 'assets/images/avatar/01.jpg',
        publishedDate: new Date(),
        readMinutes: 3,
        isFeatured: false,
        tags: [],
    };
}

export const PostDetailPage = () => {
    const { postId } = useParams();
    const navigate = useNavigate();

    const isEditing = postId != null && postId.trim().length > 0;
    const headingText = isEditing ? 'Edit post' : 'New post';

    const { data: post, isLoading: isPostLoading, isError: hasError } =
        postService.useGetPostById(postId ?? '', isEditing);

    const createPost = postService.useCreatePost();
    const updatePost = postService.useUpdatePost();
    const deletePost = postService.useDeletePost();

    const [editModel, setEditModel] = useState<PostView>(createDraft);
    const [actionError, setActionError] = useState<string | null>(null);
    const [isDeleteDialogVisible, setIsDeleteDialogVisible] = useState(false);

    const isLoading = isEditing && isPostLoading;

    useEffect(() => {
        document.title = `${headingText} — Glory 2 Him`;
    }, [headingText]);

    // Loading an existing post seeds the editor; switching to the "New" route resets it.
    useEffect(() => {
        setActionError(null);
        setEditModel(isEditing && post != null ? post : createDraft());
    }, [isEditing, post]);

    const crumbs: BreadcrumbItem[] = [
        { title: 'Admin' },
        { title: 'Posts', href: postsRoute },
        { title: isEditing ? editModel.title : 'New post', isActive: true },
    ];

    const deleteMessage = `Delete post "${editModel.title}"? This cannot be undone.`;

    const goBack = () => navigate(postsRoute);

    const savePostAsync = async () => {
        setActionError(null);

        try {
            if (isEditing) {
                await updatePost.mutateAsync(editModel);
            } else {
                await createPost.mutateAsync(editModel);
            }

            navigate(postsRoute);
        } catch (error) {
            setActionError(extractApiErrorMessage(
                error, 'The post could not be saved. Please try again.'));
        }
    };

    const openDeleteDialog = () => {
        setActionError(null);
        setIsDeleteDialogVisible(true);
    };

    const closeDeleteDialog = () => setIsDeleteDialogVisible(false);

    const confirmDeleteAsync = async () => {
        setIsDeleteDialogVisible(false);

        try {
            await deletePost.mutateAsync(editModel.id);

            navigate(postsRoute);
        } catch (error) {
            setActionError(extractApiErrorMessage(
                error, 'The post could not be deleted. Please try again.'));
        }
    };

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">{headingText}</h1>
                <Breadcrumb items={crumbs} />
            </div>
            <hr />

            {isLoading ? (
                <div className="text-center py-5">
                    <Spinner />
                </div>
            ) : hasError ? (
                <>
                    <div className="alert alert-danger" role="alert">
                        We could not load this post right now. Please try again later.
                    </div>
                    <Button color="secondary" onClick={goBack}>Back to Posts</Button>
                </>
            ) : (
                <>
                    <div className="d-flex justify-content-end mb-3">
                        <Button color="secondary" onClick={goBack}>
                            <i className="bi bi-arrow-left me-1"></i>Back to Posts
                        </Button>
                    </div>

                    {actionError != null && (
                        <div className="alert alert-danger" role="alert">{actionError}</div>
                    )}

                    <Card
                        cssClass="mb-4"
                        headerContent="Post details"
                        footerContent={
                            <div className="d-flex flex-wrap justify-content-between gap-2">
                                <div className="d-flex gap-2">
                                    <Button color="primary" onClick={() => void savePostAsync()}>Save post</Button>
                                    <Button color="secondary" onClick={goBack}>Cancel</Button>
                                </div>
                                {isEditing && (
                                    <Button color="outline-danger" onClick={openDeleteDialog}>Delete post</Button>
                                )}
                            </div>
                        }>
                        <FormText label="Title" value={editModel.title}
                            onValueChange={(value) => setEditModel({ ...editModel, title: value })} />

                        <div className="mb-3">
                            <label className="form-label">Excerpt</label>
                            <textarea
                                className="form-control"
                                rows={3}
                                value={editModel.excerpt}
                                onChange={(event) =>
                                    setEditModel({ ...editModel, excerpt: event.target.value })}></textarea>
                        </div>

                        <div className="row">
                            <div className="col-md-6">
                                <FormText label="Category" value={editModel.category}
                                    onValueChange={(value) => setEditModel({ ...editModel, category: value })} />
                            </div>
                            <div className="col-md-6">
                                <FormSelect label="Badge colour" value={editModel.categoryBadgeCss}
                                    options={badgeOptions}
                                    onValueChange={(value) =>
                                        setEditModel({
                                            ...editModel,
                                            categoryBadgeCss: value.length === 0 ? 'text-bg-primary' : value,
                                        })} />
                            </div>
                        </div>

                        <FormText label="Author" value={editModel.authorName}
                            onValueChange={(value) => setEditModel({ ...editModel, authorName: value })} />

                        <div className="row">
                            <div className="col-md-6">
                                <FormDate label="Published date" value={new Date(editModel.publishedDate)}
                                    onValueChange={(value) =>
                                        setEditModel({
                                            ...editModel,
                                            publishedDate: value ?? editModel.publishedDate,
                                        })} />
                            </div>
                            <div className="col-md-6">
                                <FormSwitch label="Featured" value={editModel.isFeatured}
                                    onValueChange={(value) => setEditModel({ ...editModel, isFeatured: value })} />
                            </div>
                        </div>
                    </Card>

                    <ConfirmDialog
                        visible={isDeleteDialogVisible}
                        title="Delete post"
                        message={deleteMessage}
                        confirmText="Delete"
                        onConfirm={() => void confirmDeleteAsync()}
                        onCancel={closeDeleteDialog} />
                </>
            )}
        </>
    );
};
