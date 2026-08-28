import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { AuthorByline } from '../components/coreUI/authorByline';
import { CommentThread } from '../components/coreUI/commentThread';
import { ContributionPrompt } from '../components/coreUI/contributionPrompt';
import { ReactionBar } from '../components/coreUI/reactionBar';
import { ShareLinks } from '../components/coreUI/shareLinks';
import { BibleReferenceAssociationPanel } from '../components/associations/bibleReferenceAssociationPanel';
import { TagAssociationPanel } from '../components/associations/tagAssociationPanel';
import { useAuth } from '../components/securitys/authProvider';
import { AssociationItem } from '../models/components/associations/associationItem';
import { ReactionOption } from '../models/coreUI/reactionOption';
import {
    asApprovedAssociations,
    asSuggestedAssociation,
    withoutAssociationValue
} from '../services/views/associations/toAssociationItems';
import {
    comments,
    detailAuthorName,
    detailComments,
    detailReactions,
    detailViews,
    featured as post,
    reactions,
} from './sampleContent';
import { useDocumentTitle } from './useDocumentTitle';
import './postSingle.css';

// The public post detail page, laid out exactly as the Post Single Magazine sample: title block,
// then the article with its reactions beside the suggestion / share sidebar, and the conversation
// below.
//
// The copy still comes from sampleContent — real posts have not been wired in yet, so the :slug
// route renders the same story whichever slug is asked for (the param is accepted by the route so
// /Post-Single/{slug} links resolve, exactly as the Blazor page did). When posts are wired in,
// only the data imports above need to change; the markup stays as it is.
export const PostSingle = () => {
    useDocumentTitle(`${post.title} — Glory 2 Him`);

    const [reactedTo, setReactedTo] = useState<string | null>(null);
    const { isAuthenticated, user } = useAuth();
    const location = useLocation();
    const loginHref = `/Account/Login?returnUrl=${encodeURIComponent(location.pathname)}`;

    const onReact = (reaction: ReactionOption) =>
        setReactedTo(reaction.label);

    // The panels own no state, so anything suggested here lives on this page — and only until it
    // is navigated away from, exactly as it did under SuggestionPanel. Wiring these to the
    // Association API is a separate job; when it lands, these two lists become the query result
    // and the handlers become mutations.
    const [suggestedTags, setSuggestedTags] = useState<ReadonlyArray<AssociationItem>>([]);

    const [suggestedReferences, setSuggestedReferences] =
        useState<ReadonlyArray<AssociationItem>>([]);

    const asSuggestion = (value: string) => asSuggestedAssociation(value, user?.userId);

    return (
        <>
            <div className="border-bottom border-primary border-1 opacity-1"></div>

            {/* Only a small gap under the header — the article's own paragraph spacing takes over
                from here, so a full section break would read as a hole in the page. */}
            <section className="pt-5 pb-3">
                <div className="container">
                    <div className="row">
                        <div className="col-12">
                            <Link to="/Categories" className={`badge ${post.categoryBadgeCss} mb-2`}>
                                <i className="fas fa-circle me-2 small fw-bold"></i>{post.category}
                            </Link>

                            <h1>{post.title}</h1>

                            {/* The author and the article's figures read along one line here
                                rather than down a rail, which frees the whole left column for the
                                sidebar. */}
                            <AuthorByline
                                authorName={detailAuthorName}
                                authorRole={post.authorRole}
                                authorImageUrl={post.authorImageUrl}
                                publishedDate={post.publishedDate}
                                readMinutes={post.readMinutes}
                                reactions={detailReactions}
                                comments={detailComments}
                                views={detailViews}
                                cssClass="mt-3" />
                        </div>
                    </div>
                </div>
            </section>

            <section className="pt-0 pb-5">
                <div className="container position-relative">
                    <div className="row">
                        <div className="col-lg-7 mb-5">
                            {/* The standfirst lives in the article column, not the full-width
                                header, so it wraps inside the text measure instead of running on
                                under the sidebar — and so it flows as an ordinary paragraph,
                                without a section break's padding stacking on top of the dropcap's
                                float clearance.
                                No .lead here: the opening paragraph reads at the same size as the
                                rest of the article, with only the dropcap setting it apart.
                                The theme's .dropcap styles a wrapping span, not ::first-letter. */}
                            <p>
                                <span className="dropcap">F</span>or all the scientists out there,
                                and for all the students who have a hard time convincing people of
                                the truth of the Bible — here is something that shows God's awesome
                                creation, and that He is still in control.
                            </p>

                            <p>
                                Did you know that the space program is busy proving that what has
                                been called "myth" in the Bible is true? Mr. Harold Hill, President
                                of the Curtis Engine Company in Baltimore, Maryland, and a
                                consultant in the space program, relates the following development.
                            </p>

                            <p>
                                Our astronauts and space scientists at Green Belt, Maryland were
                                checking the position of the sun, moon, and planets out in space —
                                where they would be 100 years and 1,000 years from now. Orbits must
                                be laid out in terms of the life of the satellite, so the whole
                                thing does not bog down.
                            </p>

                            <p>
                                They ran the computer measurement back and forth over the centuries
                                and it came to a halt. The computer stopped and put up a red
                                signal: something was wrong with either the information fed into
                                it, or the results as compared to the standards. The service
                                department found there is a day missing in space in elapsed time.
                                There was no answer.
                            </p>

                            <figure className="figure mt-2 w-100">
                                <img
                                    className="rounded w-100"
                                    src="/assets/images/blog/16by9/big/02.jpg"
                                    alt="Illustration for the article" />
                                <figcaption className="figure-caption text-center">
                                    (Image placeholder — drop in a fitting photo)
                                </figcaption>
                            </figure>

                            <p>
                                Finally, a Christian man on the team remembered Sunday School and
                                the account of the sun standing still. They were skeptical, but
                                they had no other answer — so they called for the book.
                            </p>

                            <figure className="my-4">
                                <blockquote className="blockquote">
                                    <p>
                                        The sun stood still and the moon stayed — and hasted not
                                        to go down about a whole day.
                                    </p>
                                </blockquote>
                                <figcaption className="blockquote-footer">Joshua 10:13</figcaption>
                            </figure>

                            <p>
                                There was the missing day! They checked the computers back to the
                                time it was written and found it was close but not close enough.
                                The elapsed time that was missing back in Joshua's day was 23 hours
                                and 20 minutes — not a whole day.
                            </p>

                            <p>
                                The Christian employee remembered the sun going backwards. In 2
                                Kings, Hezekiah, on his death bed, was visited by the prophet
                                Isaiah who told him he was going to die. Hezekiah asked for a sign
                                as proof, and Isaiah said "Do you want the sun to go ahead ten
                                degrees?" Hezekiah said it was nothing for the sun to go ahead ten
                                degrees — let it go backward ten degrees.
                            </p>

                            <p>
                                Ten degrees is exactly 40 minutes. Twenty-three hours and 20
                                minutes in Joshua, plus 40 minutes in 2 Kings, make the missing day
                                in the universe.
                            </p>

                            <ReactionBar reactions={reactions} onReact={onReact} />

                            {reactedTo != null && (
                                <p className="text-center small text-body-secondary mt-2 mb-0">
                                    You reacted with <strong>{reactedTo}</strong>.
                                </p>
                            )}
                        </div>

                        <div className="col-lg-5">
                            {/* Headings, prompts, the hash prefix and the search links are all
                                the component's own defaults. showModerationActions is left off,
                                which is the point of this page: a reader may suggest and withdraw
                                their own suggestion, and nothing here decides anything. */}
                            <TagAssociationPanel
                                associationCollection={[
                                    ...asApprovedAssociations(post.tags),
                                    ...suggestedTags
                                ]}
                                onAdd={(value) =>
                                    setSuggestedTags([...suggestedTags, asSuggestion(value)])}
                                onRemove={(item) =>
                                    setSuggestedTags(withoutAssociationValue(suggestedTags, item))} />

                            <hr className="my-4" />

                            {/* Each pill reads as the post cites it and addresses as the
                                deep-link route parses it, so the passage is one click away. */}
                            <BibleReferenceAssociationPanel
                                associationCollection={[
                                    ...asApprovedAssociations(post.bibleReferences),
                                    ...suggestedReferences
                                ]}
                                onAdd={(value) =>
                                    setSuggestedReferences([
                                        ...suggestedReferences,
                                        asSuggestion(value)
                                    ])}
                                onRemove={(item) =>
                                    setSuggestedReferences(
                                        withoutAssociationValue(suggestedReferences, item))} />

                            {/* No rule either side of this one — the panel's own border already
                                separates it. */}
                            <ContributionPrompt
                                cssClass="mt-4 mb-4"
                                isAuthenticated={isAuthenticated}
                                loginHref={loginHref} />

                            <hr className="my-4" />

                            <ShareLinks />
                        </div>
                    </div>

                    {/* Comments sit in their own row rather than at the foot of the article
                        column, so that when the columns stack on a phone the sidebar lands
                        between the reactions and the comments — reactions, then
                        tags/references/contribute, then the conversation last. */}
                    <div className="row">
                        <div className="col-lg-7">
                            <hr className="my-5" />

                            <CommentThread comments={comments} />
                        </div>
                    </div>
                </div>
            </section>
        </>
    );
};
