import { Link } from 'react-router-dom';
import { Button } from '../../../components/coreUI/button';
import { PodcastCard } from '../../../components/coreUI/podcastCard';
import { Spinner } from '../../../components/coreUI/spinner';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleArticleBody } from '../shared/sampleArticleBody';
import { formatLongDate } from '../shared/sampleFormats';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine podcast-single.html: one episode — cover art, transport bar, show notes and the
// rest of the series. The transport is a placeholder; no audio is wired up in these demos.
export const PodcastSingleSample = () => {
    useDocumentTitle('Podcast Single — Sample — Glory 2 Him');

    const { lead, afterLead, isLoading, isError } = useSamplePosts();

    return (
        <SampleShell title="Podcast Single" sourceFile="podcast-single.html">
            {isLoading ? (
                <div className="text-center py-5"><Spinner /></div>
            ) : isError ? (
                <div className="alert alert-danger m-4" role="alert">
                    We could not load posts right now. Please try again later.
                </div>
            ) : lead == null ? (
                <div className="alert alert-info m-4" role="alert">
                    No episodes have been published yet.
                </div>
            ) : (
                <section className="py-5">
                    <div className="container">
                        <div className="row g-4 align-items-center mb-5">
                            <div className="col-md-4">
                                <img
                                    className="rounded w-100"
                                    src={lead.imageUrl}
                                    alt={lead.title}
                                    style={{ aspectRatio: '1/1', objectFit: 'cover' }} />
                            </div>

                            <div className="col-md-8">
                                <Link to="/Categories" className={`badge ${lead.categoryBadgeCss} mb-2`}>
                                    {lead.category}
                                </Link>

                                <h1 className="h2 mb-2">{lead.title}</h1>

                                <ul className="nav nav-divider align-items-center small mb-4">
                                    <li className="nav-item">Episode 12</li>
                                    <li className="nav-item">{formatLongDate(lead.publishedDate)}</li>
                                    <li className="nav-item">32:10</li>
                                </ul>

                                <div className="card border">
                                    <div className="card-body d-flex align-items-center gap-3">
                                        <Button color="primary" cssClass="rounded-circle lh-1 p-3">
                                            <i className="bi bi-play-fill fs-4"></i>
                                        </Button>

                                        <div className="flex-grow-1">
                                            <div
                                                className="progress"
                                                style={{ height: '6px' }}
                                                role="progressbar"
                                                aria-label="Episode progress"
                                                aria-valuenow={35}
                                                aria-valuemin={0}
                                                aria-valuemax={100}>
                                                <div className="progress-bar bg-primary" style={{ width: '35%' }}></div>
                                            </div>
                                            <div className="d-flex justify-content-between small text-body-secondary mt-1">
                                                <span>11:15</span>
                                                <span>32:10</span>
                                            </div>
                                        </div>

                                        <i className="bi bi-volume-up fs-5 text-body-secondary"></i>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div className="row g-4">
                            <div className="col-lg-8">
                                <h2 className="h4 mb-3">Show notes</h2>
                                <SampleArticleBody post={lead} />
                            </div>

                            <div className="col-lg-4">
                                <h2 className="h4 mb-3">More episodes</h2>
                                <div className="vstack gap-3">
                                    {afterLead.slice(0, 4).map((post) => (
                                        <PodcastCard post={post} key={post.id} />
                                    ))}
                                </div>
                            </div>
                        </div>
                    </div>
                </section>
            )}
        </SampleShell>
    );
};
