import { ReactionOption } from '../../models/coreUI/reactionOption';
import './coreUI.css';

// "How did this post speak to you?" — a row of reaction buttons under an article.
// The parent decides what a reaction means.
export interface ReactionBarProps {
    prompt?: string;
    reactions?: ReadonlyArray<ReactionOption>;
    onReact?: (reaction: ReactionOption) => void;
}

export function ReactionBar({
    prompt = 'How did this post speak to you?',
    reactions = [],
    onReact,
}: ReactionBarProps) {
    return (
        <div className="bg-light rounded p-4 mt-5 text-center">
            <h5 className="mb-3">{prompt}</h5>

            <div className="d-flex justify-content-center flex-wrap" style={{ gap: '10px' }}>
                {reactions.map((reaction) => (
                    <button
                        key={reaction.label}
                        type="button"
                        className="reaction-btn"
                        onClick={() => onReact?.(reaction)}>
                        <i className={reaction.iconCssClass} style={{ color: reaction.color }}></i>
                        <span>{reaction.label}</span>
                        <span className="fw-bold">{reaction.count}</span>
                    </button>
                ))}
            </div>
        </div>
    );
}
