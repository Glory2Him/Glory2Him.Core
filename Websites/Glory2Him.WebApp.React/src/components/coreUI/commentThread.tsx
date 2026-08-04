import { CommentEntry } from '../../models/coreUI/commentEntry';
import { Avatar } from './avatar';
import { Button } from './button';
import { formatDateTime } from './dateFormats';
import { FormText } from './formText';
import './coreUI.css';

// Comment list with single-level replies (a reply is simply indented), followed by the reply
// form. The parent owns submission.
export interface CommentThreadProps {
    comments?: ReadonlyArray<CommentEntry>;
    showReplyForm?: boolean;
    onReact?: (comment: CommentEntry) => void;
}

export function CommentThread({ comments = [], showReplyForm = true, onReact }: CommentThreadProps) {
    return (
        <>
            <div>
                <h3 className="mb-4">{comments.length} comments</h3>

                {comments.map((comment, commentIndex) => (
                    <div
                        key={commentIndex}
                        className={`my-4 d-flex ${comment.isReply === true ? 'ps-2 ps-md-5' : ''}`}>
                        <div className="flex-shrink-0 me-3">
                            <Avatar name={comment.authorName} imageUrl={comment.authorImageUrl} sizePx={48} />
                        </div>

                        <div>
                            <div className="mb-2">
                                <h5 className="m-0">{comment.authorName}</h5>
                                <span className="me-3 small text-body-secondary">
                                    {formatDateTime(comment.postedAt)}
                                </span>
                                <a href="#reply" className="text-body fw-normal small">Reply</a>
                            </div>

                            <p className="mb-1">{comment.body}</p>

                            <button
                                type="button"
                                className="cmt-react"
                                onClick={() => onReact?.(comment)}>
                                <i className="far fa-heart me-1"></i>{comment.reactions}
                            </button>
                        </div>
                    </div>
                ))}
            </div>

            {showReplyForm && (
                <div id="reply" className="mt-5">
                    <h3 className="mb-4">Leave a reply</h3>

                    <div className="row">
                        <div className="col-md-6">
                            <FormText label="Name *" placeholder="Your name" />
                        </div>
                        <div className="col-md-6">
                            <FormText label="Email *" placeholder="you@example.com" />
                        </div>
                    </div>

                    <div className="mb-3">
                        <label className="form-label">Comment *</label>
                        <textarea className="form-control" rows={4} placeholder="Share your thoughts…"></textarea>
                    </div>

                    <Button color="primary">Post comment</Button>
                </div>
            )}
        </>
    );
}
