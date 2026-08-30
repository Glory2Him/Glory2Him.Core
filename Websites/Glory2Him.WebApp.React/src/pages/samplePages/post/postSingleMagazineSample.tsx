import { useState } from 'react';
import { Link } from 'react-router-dom';
import { AuthorByline } from '../../../components/coreUI/authorByline';
import { CommentThread } from '../../../components/coreUI/commentThread';
import { ContributionPrompt } from '../../../components/coreUI/contributionPrompt';
import { ReactionBar } from '../../../components/coreUI/reactionBar';
import { ShareLinks } from '../../../components/coreUI/shareLinks';
import { BibleReferenceAssociationPanel } from '../../../components/associations/bibleReferenceAssociationPanel';
import { TagAssociationPanel } from '../../../components/associations/tagAssociationPanel';
import { AssociationItem } from '../../../models/components/associations/associationItem';
import {
    asApprovedAssociations,
    asSuggestedAssociation,
    withoutAssociationValue
} from '../../../services/views/associations/toAssociationItems';
import { ReactionOption } from '../../../models/coreUI/reactionOption';
import {
    comments,
    detailAuthorName,
    detailComments,
    detailReactions,
    detailViews,
    featured,
    reactions,
} from '../../sampleContent';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import './postSingleMagazineSample.css';

// The Glory 2 Him post detail mockup (D:\Temp\G2H\samples\Post Detail.html): title block,
// then a three-column body — the article with its reactions and comments, and the
// suggestion / share sidebar.
export const PostSingleMagazineSample = () => {
    useDocumentTitle(`${featured.title} — Sample — Glory 2 Him`);

    const [reactedTo, setReactedTo] = useState<string | null>(null);

    // Suggestions live on the page, as they do on the real post detail. The demo passes no
    // createdBy, so a suggestion reads as pending but carries no withdraw — this page is a
    // layout showcase, not a working panel.
    const [suggestedTags, setSuggestedTags] = useState<ReadonlyArray<AssociationItem>>([]);

    const [suggestedReferences, setSuggestedReferences] =
        useState<ReadonlyArray<AssociationItem>>([]);

    const onReact = (reaction: ReactionOption): void =>
        setReactedTo(reaction.label);

    return (
        <SampleShell title="Post Single Magazine" sourceFile="Post Detail.html">
            <div className="post-single-magazine-sample">
                <div className="border-bottom border-primary border-1 opacity-1"></div>

                {/* Only a small gap under the header — the article's own paragraph spacing takes over
                    from here, so a full section break would read as a hole in the page. */}
                <section className="pt-5 pb-3">
                    <div className="container">
                        <div className="row">
                            <div className="col-12">
                                <Link to="/Categories" className={`badge ${featured.categoryBadgeCss} mb-2`}>
                                    <i className="fas fa-circle me-2 small fw-bold"></i>{featured.category}
                                </Link>

                                <h1>{featured.title}</h1>

                                {/* The author and the article's figures read along one line here rather
                                    than down a rail, which frees the whole left column for the sidebar.
                                    The detail mockup states its own counts for this story, which differ
                                    from the numbers on its Home card — each page is kept faithful to its
                                    own mockup (see detailReactions in sampleContent). */}
                                <AuthorByline
                                    authorName={detailAuthorName}
                                    authorRole={featured.authorRole}
                                    authorImageUrl={featured.authorImageUrl}
                                    publishedDate={featured.publishedDate}
                                    readMinutes={featured.readMinutes}
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
                            {/* The rail's two columns went to the sidebar, so the bible references have
                                room to sit side by side instead of stacking one per line. */}
                            <div className="col-lg-7 mb-5">
                                {/* The standfirst lives in the article column, not the full-width header,
                                    so it wraps inside the text measure instead of running on under the
                                    sidebar — and so it flows as an ordinary paragraph, without a section
                                    break's padding stacking on top of the dropcap's float clearance.
                                    No .lead here: the opening paragraph reads at the same size as the
                                    rest of the article, with only the dropcap setting it apart.
                                    The theme's .dropcap styles a wrapping span, not ::first-letter. */}
                                <p>
                                    <span className="dropcap">F</span>or all the scientists out there, and for all
                                    the students who have a hard time convincing people of the truth of the
                                    Bible — here is something that shows God's awesome creation, and that He is
                                    still in control.
                                </p>

                                <p>
                                    Did you know that the space program is busy proving that what has been
                                    called "myth" in the Bible is true? Mr. Harold Hill, President of the
                                    Curtis Engine Company in Baltimore, Maryland, and a consultant in the space
                                    program, relates the following development.
                                </p>

                                <p>
                                    Our astronauts and space scientists at Green Belt, Maryland were checking
                                    the position of the sun, moon, and planets out in space — where they would
                                    be 100 years and 1,000 years from now. Orbits must be laid out in terms of
                                    the life of the satellite, so the whole thing does not bog down.
                                </p>

                                <p>
                                    They ran the computer measurement back and forth over the centuries and it
                                    came to a halt. The computer stopped and put up a red signal: something was
                                    wrong with either the information fed into it, or the results as compared
                                    to the standards. The service department found there is a day missing in
                                    space in elapsed time. There was no answer.
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
                                    Finally, a Christian man on the team remembered Sunday School and the
                                    account of the sun standing still. They were skeptical, but they had no
                                    other answer — so they called for the book.
                                </p>

                                <figure className="my-4">
                                    <blockquote className="blockquote">
                                        <p>
                                            The sun stood still and the moon stayed — and hasted not to go down
                                            about a whole day.
                                        </p>
                                    </blockquote>
                                    <figcaption className="blockquote-footer">Joshua 10:13</figcaption>
                                </figure>

                                <p>
                                    There was the missing day! They checked the computers back to the time it
                                    was written and found it was close but not close enough. The elapsed time
                                    that was missing back in Joshua's day was 23 hours and 20 minutes — not a
                                    whole day.
                                </p>

                                <p>
                                    The Christian employee remembered the sun going backwards. In 2 Kings,
                                    Hezekiah, on his death bed, was visited by the prophet Isaiah who told him
                                    he was going to die. Hezekiah asked for a sign as proof, and Isaiah said
                                    "Do you want the sun to go ahead ten degrees?" Hezekiah said it was nothing
                                    for the sun to go ahead ten degrees — let it go backward ten degrees.
                                </p>

                                <p>
                                    Ten degrees is exactly 40 minutes. Twenty-three hours and 20 minutes in
                                    Joshua, plus 40 minutes in 2 Kings, make the missing day in the universe.
                                </p>

                                <ReactionBar reactions={reactions} onReact={onReact} />

                                {reactedTo != null && (
                                    <p className="text-center small text-body-secondary mt-2 mb-0">
                                        You reacted with <strong>{reactedTo}</strong>.
                                    </p>
                                )}
                            </div>

                            <div className="col-lg-5">
                                {/* The panel reads the viewer's identity itself, and these demos are
                                    Administrators-only, so the add box always shows here — the
                                    hardcoded isAuthenticated the old panel needed is gone. */}
                                <TagAssociationPanel
                                    chipHrefFor={(item) => `/Tag?name=${encodeURIComponent(item.value)}`}
                                    associationCollection={[
                                        ...asApprovedAssociations(featured.tags),
                                        ...suggestedTags
                                    ]}
                                    onAdd={(value) => setSuggestedTags([
                                        ...suggestedTags,
                                        asSuggestedAssociation(value, undefined)
                                    ])}
                                    onRemove={(item) =>
                                        setSuggestedTags(
                                            withoutAssociationValue(suggestedTags, item))} />

                                <hr className="my-4" />

                                {/* Every pill goes to the single-verse sample rather than the real
                                    deep link: that page exists and shows what a reference looks like
                                    when opened. It renders one fixed passage, so the reference clicked
                                    is deliberately not carried through — it would be misleading in the
                                    URL if it were. */}
                                <BibleReferenceAssociationPanel
                                    chipHrefFor={() =>
                                        '/SamplePages/BibleReferences/BibleReference-Single-verse'}
                                    associationCollection={[
                                        ...asApprovedAssociations(featured.bibleReferences),
                                        ...suggestedReferences
                                    ]}
                                    onAdd={(value) => setSuggestedReferences([
                                        ...suggestedReferences,
                                        asSuggestedAssociation(value, undefined)
                                    ])}
                                    onRemove={(item) =>
                                        setSuggestedReferences(
                                            withoutAssociationValue(suggestedReferences, item))} />

                                {/* No rule either side of this one — the panel's own border already
                                    separates it. */}
                                <ContributionPrompt
                                    href="/posts/contribute"
                                    cssClass="mt-4 mb-4"
                                    isAuthenticated={true}
                                    loginHref="/Account/Login" />

                                <hr className="my-4" />

                                <ShareLinks />
                            </div>
                        </div>

                        {/* Comments sit in their own row rather than at the foot of the article column,
                            so that when the columns stack on a phone the sidebar lands between the
                            reactions and the comments — reactions, then tags/references/contribute,
                            then the conversation last. */}
                        <div className="row">
                            <div className="col-lg-7">
                                <hr className="my-5" />

                                <CommentThread comments={comments} />
                            </div>
                        </div>
                    </div>
                </section>
            </div>
        </SampleShell>
    );
};
