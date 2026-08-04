import { ReviewCriterion } from '../../models/coreUI/reviewCriterion';

// Review scorecard for the review post layout: an overall score, a star row, and a progress bar
// per criterion.
export interface ReviewRatingProps {
    overallScore?: number;
    maximumScore?: number;
    summary?: string;
    criteria?: ReadonlyArray<ReviewCriterion>;
}

export function ReviewRating({
    overallScore = 0,
    maximumScore = 5,
    summary,
    criteria = [],
}: ReviewRatingProps) {
    const toPercentage = (score: number): number =>
        maximumScore <= 0
            ? 0
            : Math.round(Math.min(Math.max(score / maximumScore, 0), 1) * 100);

    return (
        <div className="card border">
            <div className="card-body">
                <div className="d-flex align-items-center gap-3 mb-4">
                    <div className="display-5 fw-bold text-primary lh-1">{overallScore.toFixed(1)}</div>
                    <div>
                        <div className="mb-1" role="img" aria-label={`${overallScore.toFixed(1)} out of ${maximumScore}`}>
                            {Array.from({ length: maximumScore }, (_, index) => index + 1).map((star) => (
                                <i
                                    key={star}
                                    className={`bi ${star <= Math.round(overallScore) ? 'bi-star-fill' : 'bi-star'} text-warning`}></i>
                            ))}
                        </div>
                        <div className="small text-body-secondary">{summary}</div>
                    </div>
                </div>

                {criteria.map((criterion) => (
                    <div key={criterion.label} className="mb-3">
                        <div className="d-flex justify-content-between small mb-1">
                            <span>{criterion.label}</span>
                            <span className="fw-semibold">{criterion.score.toFixed(1)}</span>
                        </div>
                        <div
                            className="progress"
                            style={{ height: '6px' }}
                            role="progressbar"
                            aria-label={criterion.label}
                            aria-valuenow={criterion.score}
                            aria-valuemin={0}
                            aria-valuemax={maximumScore}>
                            <div
                                className="progress-bar bg-primary"
                                style={{ width: `${toPercentage(criterion.score)}%` }}></div>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
}
